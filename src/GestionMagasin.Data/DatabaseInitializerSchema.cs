using Microsoft.Data.Sqlite;

namespace GestionMagasin.Data;

/// <summary>Version du schéma utilisée par les routines de sauvegarde et de restauration.</summary>
public static class DatabaseInitializerSchema
{
    public const int Current = 5;
}
