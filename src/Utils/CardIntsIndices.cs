namespace NewGamePlus;

/// <summary>
/// Index constants for the <c>Card._ints</c> array, based on the Card.cs disassembly.
/// </summary>
public static class CardIntsIndices
{
	/// <summary>_ints[1]: Card UID for containers, or spell/ability type ID for scrolls and whips.</summary>
	public const int UidOrType = 1;

	/// <summary>_ints[4]: Material ID (<c>idMaterial</c>).</summary>
	public const int IdMaterial = 4;

	/// <summary>_ints[6]: Stack quantity (<c>Num</c>).</summary>
	public const int Num = 6;

	/// <summary>_ints[25]: Item level (<c>LV</c>).</summary>
	public const int Lv = 25;
}
