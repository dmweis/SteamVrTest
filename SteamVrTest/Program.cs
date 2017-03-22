using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Valve.VR;

namespace SteamVrTest
{
   class Program
   {
      private static List<Socket> _clients = new List<Socket>();

      static void Main(string[] args)
      {
         Task.Run(() => SteamVr());
         TcpListener listener = new TcpListener(IPAddress.Any, 4242);
         listener.Start();
         while (true)
         {
            Socket client = listener.AcceptSocket();
            client.SendTimeout = 500;
            Console.WriteLine("Connected to" + client.RemoteEndPoint);
            _clients.Add(client);
         }
      }

      private static void SteamVr()
      {
         Console.WriteLine("Starting");
         EVRInitError initError = EVRInitError.None;
         CVRSystem cvrSystem = OpenVR.Init(ref initError, EVRApplicationType.VRApplication_Utility);
         Console.WriteLine("Error: " + initError.ToString());
         if (cvrSystem == null)
         {
            Console.WriteLine("Error!");
         }
         while (true)
         {
            Thread.Sleep(1);
            TrackedDevicePose_t[] trackedDevicePose = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            cvrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseRawAndUncalibrated, 0f, trackedDevicePose);
            VRControllerState_t controllerState = new VRControllerState_t();
            cvrSystem.GetControllerState(1, ref controllerState,
               (uint) System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t)));
            int trigger = controllerState.rAxis1.x > 0.9f ? 1 : 0;
            bool topButtom = (controllerState.ulButtonPressed & (1ul << (int)EVRButtonId.k_EButton_ApplicationMenu)) != 0;
            
            TrackedDevicePose_t pose = trackedDevicePose[1];
            ETrackingResult trackingResult = pose.eTrackingResult;
            HmdMatrix34_t hmdMatrix = pose.mDeviceToAbsoluteTracking;
            Position pos = new Position(hmdMatrix);
            Rotation rot = new Rotation(hmdMatrix);
            Console.WriteLine($"Position: {pos} Rotation: {rot} trigger {trigger} app {topButtom}");
            foreach (Socket client in _clients.ToArray())
            {
               try
               {
                  client.Send(Encoding.ASCII.GetBytes($"S{pos.ToData()} {rot.ToData()} {trigger} {(topButtom ? 1 : 0)}E"));

               }
               catch (Exception )
               {
                  _clients.Remove(client);
               }
            }

         }
         for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
         {
            if (cvrSystem?.IsTrackedDeviceConnected((uint)i) ?? false)
            {
               ETrackedDeviceClass deviceClass = cvrSystem.GetTrackedDeviceClass((uint)i);
               Console.WriteLine($"index: {i} is {deviceClass}");
            }
         }
         Console.ReadLine();
         OpenVR.Shutdown();
         Console.WriteLine("Shut down");
         Console.ReadLine();
      }
   }
}
