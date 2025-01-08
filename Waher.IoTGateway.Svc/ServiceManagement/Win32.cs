﻿using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.ServiceProcess;
using Waher.IoTGateway.Svc.ServiceManagement.Classes;
using Waher.IoTGateway.Svc.ServiceManagement.Enumerations;

namespace Waher.IoTGateway.Svc.ServiceManagement
{
	internal delegate void ServiceControlHandler(ServiceControlCommand control, uint eventType, IntPtr eventData, IntPtr eventContext);

	/// <summary>
	/// Handles interaction with Windows Service API.
	/// </summary>
	public static class Win32
	{
		internal const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
		internal const int ERROR_SERVICE_DOES_NOT_EXIST = 1060;

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern bool CloseServiceHandle(IntPtr handle);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern ServiceControlManager OpenSCManagerW(string machineName, string databaseName, ServiceControlManagerAccessRights dwAccess);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern ServiceHandle CreateServiceW(
			ServiceControlManager serviceControlManager,
			string serviceName,
			string displayName,
			ServiceControlAccessRights desiredControlAccess,
			ServiceType serviceType,
			ServiceStartType startType,
			ErrorSeverity errorSeverity,
			string binaryPath,
			string loadOrderGroup,
			IntPtr outUIntTagId,
			string dependencies,
			string serviceUserName,
			string servicePassword);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool ChangeServiceConfigW(
			ServiceHandle service,
			ServiceType serviceType,
			ServiceStartType startType,
			ErrorSeverity errorSeverity,
			string binaryPath,
			string loadOrderGroup,
			IntPtr outUIntTagId,
			string dependencies,
			string serviceUserName,
			string servicePassword,
			string displayName);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern ServiceHandle OpenServiceW(ServiceControlManager serviceControlManager, string serviceName,
			ServiceControlAccessRights desiredControlAccess);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern bool StartServiceW(ServiceHandle service, uint argc, IntPtr wargv);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
		internal static extern bool DeleteService(ServiceHandle service);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool ChangeServiceConfig2W(ServiceHandle service, ServiceConfigInfoTypeLevel infoTypeLevel, IntPtr info);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool QueryServiceObjectSecurity(IntPtr ServiceHandle, SecurityInfos SecurityInformation, byte[] SecurityDescriptor,
			uint BufferSize, out uint BytesNeeded);

		[DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern bool SetServiceObjectSecurity(IntPtr ServiceHandle, SecurityInfos SecurityInformation, byte[] SecurityDescriptor);

		[Flags]
		internal enum ServiceAccessRights
		{
			Start = 0x0010
		}

		// https://msdn.microsoft.com/en-us/library/windows/desktop/ms686016(v=vs.85).aspx
		// https://msdn.microsoft.com/en-us/library/windows/desktop/ms683242(v=vs.85).aspx

		[DllImport("Kernel32")]
		internal static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
		internal delegate bool HandlerRoutine(CtrlTypes CtrlType);

		// https://stackoverflow.com/questions/19487541/how-to-get-windows-user-name-from-sessionid

		[DllImport("Wtsapi32.dll")]
		internal static extern bool WTSQuerySessionInformation(IntPtr hServer, int sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);

		[DllImport("Wtsapi32.dll")]
		internal static extern void WTSFreeMemory(IntPtr pointer);


	}
}
