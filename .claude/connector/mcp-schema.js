export default function (z) {
  return [
    {
      name: "elinGameStatus",
      title: "Elin Game Status",
      description: "Check if the game-to-MCP connector is running. Returns game state info. Call this first to verify connectivity before using other elin tools.",
      schema: z.object({}),
    },
    {
      name: "elinSearchItems",
      title: "Search Elin Items",
      description: "Search the game's SourceThing registry. Matches against id, name, name_JP, and category fields. Returns summary info for each match. Use this to discover valid item IDs for recipes, components, crafting, etc.",
      schema: z.object({
        query: z.string().describe("Search term - case-insensitive substring match against id, name, name_JP, category"),
        limit: z.number().optional().default(25).describe("Max results"),
      }),
    },
    {
      name: "elinGetItem",
      title: "Get Elin Item Details",
      description: "Get full details for an item by exact ID. Returns all fields: components, factory, trait, elements, category, value, weight, recipe info, etc.",
      schema: z.object({
        id: z.string().describe("Exact item ID (e.g., 'sugar', 'log', 'apple')"),
      }),
    },
    {
      name: "elinListCategories",
      title: "List Elin Item Categories",
      description: "List all unique item categories in the game with item counts. Use to find valid category values.",
      schema: z.object({}),
    },
    {
      name: "elinGiveItem",
      title: "Give Elin Item",
      description: "Spawn an item into the player's inventory. Use elinSearchItems to find valid item IDs first.",
      schema: z.object({
        id: z.string().describe("Exact item ID (e.g., 'mysweetelin_sugar', 'sugar', 'log')"),
        count: z.number().optional().default(1).describe("Quantity to give"),
      }),
    },
    {
      name: "elinSearchElements",
      title: "Search Elin Elements",
      description: "Search the game's element registry (feats, skills, attributes, abilities). Returns id, name, category, group, tags. Use to discover valid element IDs.",
      schema: z.object({
        query: z.string().describe("Search term - case-insensitive substring match against id, name, category"),
        category: z.string().optional().describe("Filter by category: 'feat', 'skill', 'attribute', 'ability'"),
      }),
    },
    {
      name: "elinElement",
      title: "Get/Set Elin Element on PC",
      description: "Get or set an element (skill, attribute, etc.) on the player character by numeric ID. If value is provided, sets vBase via SetBase.",
      schema: z.object({
        elementId: z.number().describe("Numeric element ID"),
        value: z.number().optional().describe("If provided, set vBase to this value"),
      }),
    },
    {
      name: "elinFeat",
      title: "Get/Set Elin Feat on PC",
      description: "Get or set a feat on the player character by numeric ID. If tier is provided, sets the feat via SetFeat (handles Apply/Unapply properly).",
      schema: z.object({
        featId: z.number().describe("Numeric feat element ID"),
        tier: z.number().optional().describe("If provided, set feat to this tier"),
      }),
    },
    {
      name: "elinStat",
      title: "Get/Set Elin Character Stat",
      description: "Get or set a character stat field on the player character. Fields: level, feat, race, job, hp, karma, fame, corruption, faith, exp. If no field provided, returns all fields.",
      schema: z.object({
        field: z.string().describe("Stat field name: level, feat, race, job, hp, karma, fame, corruption, faith, exp"),
        value: z.string().optional().describe("If provided, set the field to this value (string to support both numbers and IDs like race/job/faith)"),
      }),
    },
    {
      name: "elinGenes",
      title: "List Elin PC Genes",
      description: "List all installed genes on the player character. Returns gene details including element values.",
      schema: z.object({}),
    },
  ];
}
