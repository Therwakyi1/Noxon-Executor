#pragma once
#include <ntddk.h>

#define DRIVER_NAME L"NoxonDriver"
#define DEVICE_NAME L"\\Device\\NoxonDriver"
#define SYMLINK_NAME L"\\DosDevices\\NoxonDriver"

NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath);
VOID DriverUnload(PDRIVER_OBJECT DriverObject);

NTSTATUS DisableHyperionCallbacks();
NTSTATUS SpoofMemoryChecks();
NTSTATUS BypassDSE();
