using System;

namespace LiteDB.Studio.Mvvm.Hosting;

public sealed record ViewRegistration(Type ViewType, Type ViewModelType);
