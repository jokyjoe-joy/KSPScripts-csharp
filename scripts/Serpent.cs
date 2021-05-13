using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using KSPScripts;
class Serpent
{
    public static Connection conn;
    public static KRPC.Client.Services.SpaceCenter.Service spaceCenter;
    public static Vessel vessel;
    public static Flight flight;
    public static ReferenceFrame orbitBodyReferenceFrame;
    public static Stream<Tuple<double,double,double>> positionStream;
    public static Stream<float> solidFuelStream;
    public static Stream<double> meanAltitudeStream;
    public static Stream<double> apoapsisAltitudeStream;
    public static Stream<double> periapsisAltitudeStream;
    public static Stream<double> surfaceAltitudeStream;
    public static Stream<double> verticalSpeedStream;
    public static Event landingFailsafeEvent;
    public static Tuple<double, double, double> PROGRADE = Tuple.Create (0.0, 1.0, 0.0);
    public static Tuple<double, double, double> Retrograde = Tuple.Create (0.0, -1.0, 0.0);
    public static Double OrbitApoapsisAlt;
    public static Double OrbitPeriapsisAlt;
    public static IProgress<string> progress;

    // TODO: Rewrite whole script to use adaptive throttling.
    /// <summary>
    /// 
    /// </summary>
    public static void Start(double orbitApoapsisAlt = 72000, double orbitPeriapsisAlt = 72000, bool consoleToGUI = true, dynamic consoleBoxGUI = null, IProgress<string> _progress = null)
    {
        OrbitApoapsisAlt = orbitApoapsisAlt;
        OrbitPeriapsisAlt = orbitPeriapsisAlt;
        progress = _progress;

        // Redirect Console.WriteLine to GUI.
        if (consoleToGUI && consoleBoxGUI != null) Console.SetOut(new KSPScripts.App.GUIConsoleWriter(consoleBoxGUI));
        // Check if given options are appropriate.
        if (OrbitApoapsisAlt < 70000 || OrbitPeriapsisAlt < 70000)
        {
            progress.Report("Given orbit altitudes are incorrect, as they are below 70000 meters.");
            return;
        }

        conn = Utilities.Connect("Serpent");
        if (conn == null)
        {
            progress.Report("Couldn't connect to the kRPC server. Exiting...");
            return;
        }

        // Getting variables and streams
        spaceCenter = conn.SpaceCenter();
        vessel = spaceCenter.ActiveVessel;
        // Use surface reference frame as we are still on Kerbin
        flight = vessel.Flight(vessel.SurfaceReferenceFrame);
        orbitBodyReferenceFrame = vessel.Orbit.Body.ReferenceFrame;

        // Setting up streams here so that it will be easier to refer to them later on.
        positionStream = conn.AddStream(() => vessel.Position(orbitBodyReferenceFrame));
        solidFuelStream = conn.AddStream(() => vessel.Resources.Amount("SolidFuel"));
        meanAltitudeStream = conn.AddStream(() => flight.MeanAltitude);
        apoapsisAltitudeStream = conn.AddStream(() => vessel.Orbit.ApoapsisAltitude);
        periapsisAltitudeStream = conn.AddStream(() => vessel.Orbit.PeriapsisAltitude);
        surfaceAltitudeStream = conn.AddStream(() => flight.SurfaceAltitude);
        verticalSpeedStream = conn.AddStream(() => vessel.Flight(orbitBodyReferenceFrame).VerticalSpeed);

        // Setup events
        // When going down vertically call landing stage. 
        var verticalSpeedCall = Connection.GetCall(() => vessel.Flight(orbitBodyReferenceFrame).VerticalSpeed);
        Expression expr = Expression.LessThan(conn,Expression.Call(conn, verticalSpeedCall), Expression.ConstantDouble(conn, 0));
        landingFailsafeEvent = conn.KRPC().AddEvent(expr);
        landingFailsafeEvent.AddCallback(LandingStageLoop);

        // Start sequence
        Utilities.Countdown(5, progress);
        LaunchStageLoop();
        SuborbitalStageLoop();
        progress.Report("Launch stage concluded.");
    }

    /// <summary>
    /// Has 3 ActivateNextStage() calls.
    /// </summary>
    public static void LaunchStageLoop()
    {
        // Setting up auto-pilot.
        vessel.AutoPilot.TargetPitchAndHeading(90, 90);
        vessel.AutoPilot.Engage();
        vessel.Control.Throttle = 1;

        // Launch!
        progress.Report("Launch");
        vessel.Control.ActivateNextStage();
     
        // Execute gravity turn when reaching 10000 meters.
        // TODO: What if vessel runs out of fuel before reaching 10000 meters?
        while (meanAltitudeStream.Get() <= 10000)
        {
            System.Threading.Thread.Sleep(100);
        }
        // Starting landingFailsafe here, as we already have vertical speed.
        landingFailsafeEvent.Start();

        progress.Report("Gravity turn");
        vessel.AutoPilot.TargetPitchAndHeading(60, 90);

        // Wait until out of solid fuel...
        while (solidFuelStream.Get() > 0.1f)
            System.Threading.Thread.Sleep(100);

        vessel.Control.Throttle = 0;
        vessel.Control.ActivateNextStage();
        // Wait a little with no throttle, so that the craft's boosters don't collide and explode.
        System.Threading.Thread.Sleep(2000);
        vessel.Control.Throttle = 1;

        // Wait until out of liquid fuel...
        // CurrentStage - 1, because it will get decoupled in the next stage, not in the current one.
        while (vessel.ResourcesInDecoupleStage(vessel.Control.CurrentStage - 1).Amount("LiquidFuel") > 0.1f)
            System.Threading.Thread.Sleep(100);

        vessel.Control.ActivateNextStage();
        progress.Report("Ending launch stage...");
    }
    public static void SuborbitalStageLoop()
    {
        progress.Report("Entering suborbital stage");
        // Waiting until having an apoapsis of specified meters.
        while (apoapsisAltitudeStream.Get() < 72000)
            System.Threading.Thread.Sleep(100);
        vessel.Control.Throttle = 0;
        
        vessel.AutoPilot.ReferenceFrame = vessel.OrbitalReferenceFrame;

        // Waiting until approaching apoapsis, then throttle in prograde
        while (meanAltitudeStream.Get() < apoapsisAltitudeStream.Get() - 4000)
        {
            vessel.AutoPilot.TargetDirection = PROGRADE;
            System.Threading.Thread.Sleep(100);
        }
        // Waiting until reaching specified periapsis altitude.
        while (periapsisAltitudeStream.Get() < 72000)
        {
            // Only throttle when within 3 mins of apoapsis.
            if (vessel.Orbit.TimeToApoapsis < 180)
                vessel.Control.Throttle = 1;
            else if (vessel.Orbit.TimeToApoapsis > 700) // Keep throttling in case we are over apoapsis and not in orbit.
                vessel.Control.Throttle = 1;
            else 
            {
                vessel.Control.Throttle = 0;
                // Warp 20 s before apoapsis.
                spaceCenter.WarpTo(spaceCenter.UT + vessel.Orbit.TimeToApoapsis - 20);
            }
            System.Threading.Thread.Sleep(100);
        }
        vessel.Control.Throttle = 0;

        // TODO: Set up orbit to orbitApoapsisAlt nad orbitPeriapsisAlt.

        // Remove landingFailsafe as we are in orbit
        landingFailsafeEvent.Remove();
        progress.Report("Exiting suborbital stage");
    }
    /// <summary>
    /// Has 1 ActivateNextStage() call.
    /// Has 1 LandingStageLoop() call.
    /// </summary>
    public static void DeorbitalStageLoop()
    {
        progress.Report("Entering deorbital stage");

        vessel.AutoPilot.Engage();
        // TODO: Check if apoapsis or periapsis is bigger, and burn retrograde accordingly
        vessel.AutoPilot.ReferenceFrame = vessel.OrbitalReferenceFrame;

        // Warping 40 s before apoapsis.
        spaceCenter.WarpTo(spaceCenter.UT + vessel.Orbit.TimeToApoapsis - 40);
        // Waiting until approaching apoapsis, then throttle in retrograde
        while (vessel.Orbit.TimeToApoapsis > 20)
        {
            vessel.AutoPilot.TargetDirection = Retrograde;
            System.Threading.Thread.Sleep(100);
        }
        // Only throttle when looking at retrograde with 3° error.
        while (vessel.AutoPilot.Error > 3)
            System.Threading.Thread.Sleep(100);

        // Burn in retrograde until reaching an altitude low enough to land
        vessel.Control.Throttle = 1;
        while (vessel.Orbit.PeriapsisAltitude > 40000)
        {
            vessel.AutoPilot.TargetDirection = Retrograde;
            System.Threading.Thread.Sleep(100);
        }
        vessel.Control.Throttle = 0;
        // Wait a few secs, because the throttle doesn't go away instantly if activating next stage :/
        System.Threading.Thread.Sleep(3000); 

        vessel.Control.ActivateNextStage();
        progress.Report("Exiting deorbital stage");
        // TODO: Warp to around 90k meters?
        LandingStageLoop();
    }
    public static void LandingStageLoop()
    {
        progress.Report("Entering landing stage");
        // Check if there is a parachute in current stage, if there isn't, activate next stage
        // TODO: Activate stages until we have parachute?
        bool hasParachuteInStage = false;
        foreach (var part in vessel.Parts.InStage(vessel.Control.CurrentStage))
        {
            if (part.Parachute != null) // TODO: Doesn't work
            {
                hasParachuteInStage = true;
                break;
            }
        }
        if (!hasParachuteInStage) vessel.Control.ActivateNextStage();

        vessel.AutoPilot.Engage();
        vessel.AutoPilot.ReferenceFrame = vessel.SurfaceReferenceFrame;
        // Targeting retrograde while entering atmosphere (for the heatshields).
        while (surfaceAltitudeStream.Get() > 2000)
        {
            vessel.AutoPilot.TargetDirection = vessel.Flight(null).Retrograde;
            System.Threading.Thread.Sleep(100);
        }
        
        progress.Report("Deploying parachutes.");
        foreach (Parachute parachute in vessel.Parts.Parachutes)
            parachute.Deploy();
    }
    public static void CollectExperimentResults(IProgress<string> _progress = null)
    {
        // Use Serpent's progress for logging in case it exists.
        // Otherwise use the one that is given here.
        if (progress != null && _progress == null)
            _progress = progress;
        // In case Serpent's progress is null and _progress is not given, return with error.
        else if (_progress == null)
        {   
            // TODO: Use logging here some way...
            return;
        }

        // Connect in case it is called from another script, or it is
        // called before Start().
        if (conn == null)
        {
            conn = Utilities.Connect("Experiments");
            if (conn == null)
            {
                // TODO: This way it is not logging?
                _progress.Report("Couldn't connect to the kRPC server. Exiting...");
                return;
            }
            vessel = conn.SpaceCenter().ActiveVessel;
        }

        _progress.Report("Collecting experiments");

        foreach (Experiment experiment in vessel.Parts.Experiments)
        {
            if (!experiment.HasData)
            {
                experiment.Run();
                string partName = experiment.Part.Name.Replace("."," ").FirstCharToUpper();
                _progress.Report($"Experiment successfully collected from {partName}.");
                // Wait a little as Data is empty :/
                System.Threading.Thread.Sleep(200);
                foreach (ScienceData data in experiment.Data)
                    _progress.Report($"{data.ScienceValue} science collected.");
            }
        }
    }
}