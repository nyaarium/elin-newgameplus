namespace NewGamePlus;

// Constants for Card._ints array indices
// Based on Card.cs disassembly - these indices map to Card properties
public static class CardIntsIndices
{
	// _ints[1] = uid (for containers) or type (spell/ability ID for scrolls/whips)
	public const int UidOrType = 1;
	
	// _ints[4] = idMaterial
	public const int IdMaterial = 4;
	
	// _ints[6] = Num (quantity)
	public const int Num = 6;
	
	// _ints[25] = LV (level)
	public const int Lv = 25;
}
