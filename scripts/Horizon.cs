using System;
using KRPC.Client;
using KRPC.Client.Services.KRPC;
using KRPC.Client.Services.SpaceCenter;
using KSPScripts;

class Horizon
{
    public static Connection conn;
    public static KRPC.Client.Services.SpaceCenter.Service spaceCenter;
    public static Vessel vessel;
    public static Flight flight;
    public static ReferenceFrame orbitBodyReferenceFrame;
    public static IProgress<string> progress;
    public static Stream<Tuple<double,double,double>> positionStream;
    public static Stream<float> solidFuelStream;
    public static Stream<double> meanAltitudeStream;
    public static Stream<double> apoapsisAltitudeStream;
    public static Stream<double> periapsisAltitudeStream;
    public static Stream<double> surfaceAltitudeStream;
    public static Stream<double> verticalSpeedStream;
    public static void Start(bool consoleToGUI = true, dynamic consoleBoxGUI = null, IProgress<string> _progress = null)
    {
        progress = _progress;
        // Redirect Console.WriteLine to GUI.
        if (consoleToGUI && consoleBoxGUI != null) Console.SetOut(new KSPScripts.App.GUIConsoleWriter(consoleBoxGUI));
        
        conn = Utilities.Connect("Horizon");
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

        // Start landing.
        Land();
    }

    public static void Land()
    {
        progress.Report("Initiating landing sequence.");
        // Wait for negative vertical speed.
        while (verticalSpeedStream.Get() > 0)
            System.Threading.Thread.Sleep(100);
        
        progress.Report("Starting landing sequence.");
        vessel.AutoPilot.ReferenceFrame = vessel.OrbitalReferenceFrame;
        vessel.AutoPilot.TargetDirection = Tuple.Create (0.0, -1.0, 0.0);
        vessel.AutoPilot.Engage();

        while (verticalSpeedStream.Get() > 0)
        {
            Console.WriteLine(verticalSpeedStream.Get());
            System.Threading.Thread.Sleep(50);
        }
    }

}