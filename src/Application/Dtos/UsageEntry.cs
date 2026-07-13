using Domain.Enums;

namespace Application.Dtos;

public sealed record UsageEntry(
    string Provider,
    string Operation,
    UsageUnit Unit,
    long InputUnits,
    long OutputUnits = 0,
    Guid? JobId = null);
