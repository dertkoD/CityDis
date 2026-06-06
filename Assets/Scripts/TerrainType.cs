// Resource / terrain types used by tile sections.
// Kept at 4 types for the current prototype. Extra Dorfromantik types
// (Trees, Houses, Fields, WaterBody) can be appended later WITHOUT reordering
// the existing values, so serialized assets keep working.
public enum TerrainType
{
    Plain = 0,
    River = 1,
    Railroad = 2,
	Forest = 3,
	WaterBody = 4,
	City = 5, 
	Mountains = 6,
    // City placeholder. May only appear in the CENTER of a tile (never on an edge).
    Empty = 7
}
