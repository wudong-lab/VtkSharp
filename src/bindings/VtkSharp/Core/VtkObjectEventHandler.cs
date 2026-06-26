namespace VtkSharp;

public delegate void VtkObjectEventHandler(vtkObject caller, uint eventId);

public delegate void VtkObjectEventDataHandler(vtkObject caller, uint eventId, object? clientData, nint callData);
