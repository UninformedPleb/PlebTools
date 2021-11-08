using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.DisplayDevices;

namespace Disposition
{
	class Program
	{
		private const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
		private const uint EDS_RAWMODE = 2;

		static unsafe void Main(string[] args)
		{
			DISPLAY_DEVICEW dd = new DISPLAY_DEVICEW();
			dd.cb = (uint)Marshal.SizeOf(dd);
			DEVMODEW dm = new DEVMODEW();
			dm.dmSize = (ushort)Marshal.SizeOf(dm);
			dm.dmDriverExtra = 0;

			if(args.Length == 0)
			{
				for(uint deviceID = 0; PInvoke.EnumDisplayDevices(null, deviceID, ref dd, EDD_GET_DEVICE_INTERFACE_NAME); deviceID++)
				{
					if(!PInvoke.EnumDisplaySettingsEx(dd.DeviceName.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref dm, EDS_RAWMODE))
					{
						Console.WriteLine($"Display[{deviceID}]: {dd.DeviceName}, Unable to read display settings.");
						continue;
					}

					Console.WriteLine($"Display[{deviceID}]: {dd.DeviceName}, Position: ({dm.Anonymous1.Anonymous2.dmPosition.x}, {dm.Anonymous1.Anonymous2.dmPosition.y})");
				}
			}
			else
			{
				for(int x = 0; x < args.Length; x+=3)
				{
					uint deviceIndex = uint.Parse(args[x]);
					int xPos = int.Parse(args[x+1]);
					int yPos = int.Parse(args[x+2]);

					Console.WriteLine($"Applying position ({xPos}, {yPos}) to monitor {deviceIndex}");

					if(!PInvoke.EnumDisplayDevices(null, deviceIndex, ref dd, EDD_GET_DEVICE_INTERFACE_NAME))
					{
						Console.WriteLine("Unable to connect to display.");
						continue;
					}
					if(!PInvoke.EnumDisplaySettingsEx(dd.DeviceName.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref dm, EDS_RAWMODE))
					{
						Console.WriteLine("Unable to read display settings.");
						continue;
					}
					dm.dmFields = (uint)DeviceModeFields.Position;
					dm.Anonymous1.Anonymous2.dmPosition.x = xPos;
					dm.Anonymous1.Anonymous2.dmPosition.y = yPos;
					if(PInvoke.ChangeDisplaySettingsEx(dd.DeviceName.ToString(), dm, (HWND)IntPtr.Zero, CDS_TYPE.CDS_GLOBAL | CDS_TYPE.CDS_UPDATEREGISTRY, (void*)IntPtr.Zero) == DISP_CHANGE.DISP_CHANGE_SUCCESSFUL)
					{
						Console.WriteLine("Success!");
					}
					else
					{
						Console.WriteLine("Unable to write to display settings.");
						continue;
					}
				}
			}
		}
	}
}
