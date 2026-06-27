namespace VtkSharp;

public delegate void VtkObjectEventDataHandler(vtkObject caller, uint eventId, object? clientData, nint callData);