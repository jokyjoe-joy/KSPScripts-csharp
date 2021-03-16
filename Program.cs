using System;
using System.Net;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
class Program
{
    public static Connection conn;
    public static KRPC.Client.Services.SpaceCenter.Service spaceCenter;
    public static Vessel vessel;
    public static Flight flight;
    public static ReferenceFrame referenceFrame;
    public static Stream<Tuple<double,double,double>> positionStream;
    public static Stream<float> solidFuelStream;
    public static Stream<double> meanAltitudeStream;
    public static Stream<double> apoapsisAltitudeStream;
    public static Stream<double> periapsisAltitudeStream;
    public static Stream<double> surfaceAltitudeStream;
    public static Stream<double> verticalSpeedStream;
    public static Event landingFailsafeEvent;

    public static void Main()
    {
        // Decided that having a proper error msg when can't connect would be better.
        try 
        {
            conn = new Connection();
        }
        catch
        {
            Console.WriteLine("Couldn't connect to the kRPC server. Exiting...");
            return;
        }
        
        // Getting variables and streams
        spaceCenter = conn.SpaceCenter();
        vessel = spaceCenter.ActiveVessel;
        flight = vessel.Flight(null); // TODO: refFrame? autopilot directions don't work this way
        referenceFrame = vessel.Orbit.Body.ReferenceFrame;

        // Setting up streams here so that it will be easier to refer to them later on.
        positionStream = conn.AddStream(() => vessel.Position(referenceFrame));
        solidFuelStream = conn.AddStream(() => vessel.Resources.Amount("SolidFuel"));
        meanAltitudeStream = conn.AddStream(() => flight.MeanAltitude);
        apoapsisAltitudeStream = conn.AddStream(() => vessel.Orbit.ApoapsisAltitude);
        periapsisAltitudeStream = conn.AddStream(() => vessel.Orbit.PeriapsisAltitude);
        surfaceAltitudeStream = conn.AddStream(() => flight.SurfaceAltitude);
        verticalSpeedStream = conn.AddStream(() => vessel.Flight(referenceFrame).VerticalSpeed);
        

        // Setup events
        // When going down vertically call landing stage. 
        var verticalSpeedCall = Connection.GetCall(() => vessel.Flight(referenceFrame).VerticalSpeed);
        Expression expr = Expression.LessThan(conn,Expression.Call(conn, verticalSpeedCall), Expression.ConstantDouble(conn, 0));
        landingFailsafeEvent = conn.KRPC().AddEvent(expr);
        landingFailsafeEvent.AddCallback(() => LandingStageLoop());
        landingFailsafeEvent.Start();

        Countdown(5);
        LaunchStageLoop();
        SuborbitalStageLoop();
        LandingStageLoop();
    }

    public static void Countdown(int x)
    {
        for (int i = x; i > 0; i--)
        {
            Console.WriteLine(i);
            System.Threading.Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Has 2 ActivateNextStage() calls.
    /// </summary>
    public static void LaunchStageLoop()
    {
        // Setting up auto-pilot.
        vessel.AutoPilot.TargetPitchAndHeading(90, 90);
        vessel.AutoPilot.Engage();
        vessel.Control.Throttle = 1;

        // Launch!
        Console.WriteLine("Launch");
        vessel.Control.ActivateNextStage();
        
     
        // Execute gravity turn when reaching 100 m/s in vertical speed.
        while (verticalSpeedStream.Get() <= 100)
            continue;
        
        Console.WriteLine("Gravity turn");
        vessel.AutoPilot.TargetPitchAndHeading(60, 90);

        // Wait until out of fuel...
        while (solidFuelStream.Get() > 0.1f)
            continue;

        Console.WriteLine("Ending launch stage...");
        vessel.Control.ActivateNextStage();
    }

    /// <summary>
    /// Has 1 ActivateNextStage() call.
    /// Has 1 CollectExperiemntResults() call.
    /// </summary>
    public static void SuborbitalStageLoop()
    {
        Console.WriteLine("Entering suborbital stage");
        // Waiting until having an apoapsis of 72000 meters.
        while (apoapsisAltitudeStream.Get() < 72000)
            continue;
        vessel.Control.Throttle = 0;
        
        // Waiting until approaching apoapsis, then throttling prograde
        while (meanAltitudeStream.Get() < apoapsisAltitudeStream.Get() - 2000)
            vessel.AutoPilot.TargetDirection = flight.Prograde;

        CollectExperimentResults();
        vessel.Control.Throttle = 1;

        while (periapsisAltitudeStream.Get() < 72000)
            continue;

        vessel.Control.Throttle = 0;

        Console.WriteLine("Exiting suborbital stage");
        vessel.Control.ActivateNextStage();
    }
    public static void LandingStageLoop()
    {
        Console.WriteLine("Entering landing stage");
        // Targeting retrograde while entering atmosphere (for the heatshields).
        while (surfaceAltitudeStream.Get() > 2000)
            vessel.AutoPilot.TargetDirection = flight.Retrograde;

        
        Console.WriteLine("Deploying parachutes.");
        foreach (Parachute parachute in vessel.Parts.Parachutes)
            parachute.Deploy();
    }

    public static void CollectExperimentResults()
    {
        Console.WriteLine("Collecting experiments");
        foreach (Experiment experiment in vessel.Parts.Experiments)
            experiment.Run();
    }
}