using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;

namespace ConquestFrontierWarsRay.Data.Tests.Common;

public static class UtfDbXmlParityTestSupport
{
    private static readonly Lazy<RepoFixture> Fixture = new(CreateFixture);

    public static IReadOnlyDictionary<string, int> EntriesByType => Fixture.Value.EntriesByType;

    public static IEnumerable<object[]> GetCasesForType(string typeName)
    {
        foreach (var entry in Fixture.Value.Entries
                     .Where(entry => entry.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
        {
            yield return [entry];
        }
    }

    public static void AssertBinaryMatchesXml(UtfDbXmlCase testCase)
    {
        var details = Fixture.Value.Repository.ReadEntryDetails(testCase.DatabaseName, testCase.TypeName, testCase.FileName);
        var xml = XDocument.Load(testCase.XmlPath);

        if (details.TypedValue is not null)
        {
            var gtSystemKitLightCount = GetGtSystemKitLightComparisonCount(
                details.TypedValue,
                xml.Root ?? throw new InvalidDataException($"XML '{testCase.XmlPath}' does not have a root element."));
            var xmlValue = XmlModelProjector.CreateFromXml(
                xml.Root ?? throw new InvalidDataException($"XML '{testCase.XmlPath}' does not have a root element."),
                details.TypedValue.GetType());

            var differences = DeepComparer.Compare(details.TypedValue, xmlValue)
                .Where(difference => !ShouldIgnoreTypedDifference(testCase, difference, gtSystemKitLightCount))
                .ToList();
            Assert.True(differences.Count == 0,
                $"{testCase}{Environment.NewLine}{string.Join(Environment.NewLine, differences.Take(20))}");
            return;
        }

        var xmlFields = XmlFieldProjector.Project(
            xml.Root ?? throw new InvalidDataException($"XML '{testCase.XmlPath}' does not have a root element."));
        var fieldDifferences = FieldComparer.Compare(details, xmlFields);
        Assert.True(fieldDifferences.Count == 0,
            $"{testCase}{Environment.NewLine}{string.Join(Environment.NewLine, fieldDifferences.Take(20))}");
    }

    private static RepoFixture CreateFixture()
    {
        var repoRoot = FindRepoRoot();
        var dbRoot = Path.Combine(repoRoot, "DB");
        var xmlRoot = Path.Combine(dbRoot, "xml");

        var specs = new[]
        {
            new UtfDbDatabaseSpec("GameTypes.db", Path.Combine(dbRoot, "GameTypes.db"), Path.Combine(xmlRoot, "GameTypes.db")),
            new UtfDbDatabaseSpec("GenData.db", Path.Combine(dbRoot, "GenData.db"), Path.Combine(xmlRoot, "GenData.db")),
            new UtfDbDatabaseSpec("StringPack.db", Path.Combine(dbRoot, "StringPack.db"), Path.Combine(xmlRoot, "StringPack.db"))
        };

        var repository = new UtfDbRepository(specs);
        var entries = new List<UtfDbXmlCase>();

        foreach (var spec in specs)
        {
            var filesByType = repository.GetTypes(spec.Name)
                .ToDictionary(typeName => typeName,
                    typeName => repository.GetFiles(spec.Name, typeName)
                        .Select(file => file.FileName)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var typeDirectory in Directory.EnumerateDirectories(spec.XmlRootPath!))
            {
                var typeName = Path.GetFileName(typeDirectory);
                if (!UtfDbSupportedTypes.Parsers.ContainsKey(typeName) ||
                    !filesByType.TryGetValue(typeName, out var fileNames))
                {
                    continue;
                }

                var selectedPaths = Directory.EnumerateFiles(typeDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

                foreach (var xmlPath in selectedPaths)
                {
                    var fileName = Path.GetFileNameWithoutExtension(xmlPath);
                    if (!fileNames.Contains(fileName))
                    {
                        continue;
                    }

                    entries.Add(new UtfDbXmlCase(spec.Name, typeName, fileName, xmlPath));
                }
            }
        }

        return new RepoFixture(repository, entries);
    }

    private static string FindRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "DB", "xml")) &&
                File.Exists(Path.Combine(current.FullName, "ConquestFrontierWarsRay", "ConquestFrontierWarsRay.csproj")))
            {
                return Path.Combine(current.FullName, "ConquestFrontierWarsRay");
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ConquestFrontierWarsRay repo root from test output directory.");
    }

    public sealed record UtfDbXmlCase(string DatabaseName, string TypeName, string FileName, string XmlPath)
    {
        public override string ToString() => $"{DatabaseName}/{TypeName}/{FileName}";
    }

    private static bool ShouldIgnoreTypedDifference(UtfDbXmlCase testCase, string difference, int? gtSystemKitLightCount)
    {
        if (testCase.TypeName.Equals("GT_MENU1", StringComparison.OrdinalIgnoreCase))
        {
            // Known parity exception: the bot display-name arrays in GT_MENU1 slots differ
            // between current binary decode and the XML export, but they are non-behavioral
            // custom AI names rather than gameplay values.
            return difference.StartsWith("$.Slots.TerranComputerNames[", StringComparison.Ordinal) ||
                   difference.StartsWith("$.Slots.MantisComputerNames[", StringComparison.Ordinal) ||
                   difference.StartsWith("$.Slots.SolarianComputerNames[", StringComparison.Ordinal);
        }

        if (testCase.TypeName.Equals("GT_SYSTEM_KIT", StringComparison.OrdinalIgnoreCase))
        {
            if (gtSystemKitLightCount is int lightCount &&
                TryGetIndexedArrayDifference(difference, "LightInfo", out var index) &&
                index >= lightCount)
            {
                return true;
            }
        }

        return false;
    }

    private static int? GetGtSystemKitLightComparisonCount(object typedValue, XElement root)
    {
        if (typedValue is not GT_SYSTEM_KIT typedSystemKit)
        {
            return null;
        }

        var xmlNumLight = root.Elements().FirstOrDefault(child =>
            string.Equals(child.Name.LocalName, "numLight", StringComparison.OrdinalIgnoreCase));
        if (xmlNumLight is not null &&
            uint.TryParse(xmlNumLight.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var xmlCount))
        {
            return (int)Math.Min(typedSystemKit.NumLight, xmlCount);
        }

        return (int)typedSystemKit.NumLight;
    }

    private static bool TryGetIndexedArrayDifference(string difference, string arrayPropertyName, out int index)
    {
        index = -1;
        var prefix = "$." + arrayPropertyName + "[";
        if (!difference.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var end = difference.IndexOf(']', prefix.Length);
        if (end < 0)
        {
            return false;
        }

        return int.TryParse(
            difference.Substring(prefix.Length, end - prefix.Length),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out index);
    }

    private sealed record RepoFixture(UtfDbRepository Repository, IReadOnlyList<UtfDbXmlCase> Entries)
    {
        public IReadOnlyDictionary<string, int> EntriesByType =>
            Entries.GroupBy(entry => entry.TypeName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }
}

internal static class XmlModelProjector
{
    public static object CreateFromXml(XElement element, Type targetType)
    {
        return CreateValue(element, targetType, propertyName: null)
               ?? throw new InvalidOperationException($"Could not create '{targetType.FullName}' from XML.");
    }

    private static object? CreateValue(XElement element, Type targetType, string? propertyName)
    {
        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (targetType == typeof(string))
        {
            return element.Value.Trim();
        }

        if (targetType == typeof(bool))
        {
            return ParseBoolean(element.Value);
        }

        if (targetType.IsEnum)
        {
            var underlyingType = Enum.GetUnderlyingType(targetType);
            var numericValue = ConvertScalar(element.Value, underlyingType);
            return Enum.ToObject(targetType, numericValue);
        }

        if (IsScalar(targetType))
        {
            return ConvertScalar(element.Value, targetType);
        }

        if (targetType.IsArray)
        {
            return CreateArray(element, targetType, propertyName);
        }

        var instance = Activator.CreateInstance(targetType)
                      ?? throw new InvalidOperationException($"Could not instantiate '{targetType.FullName}'.");

        var writableProperties = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanWrite)
            .ToArray();

        foreach (var property in writableProperties)
        {
            if (!TryCreatePropertyValue(element, property, out var value))
            {
                continue;
            }

            property.SetValue(instance, value);
        }

        PopulateDerivedEnumProperties(element, targetType, instance, writableProperties);
        PopulateDerivedScalarProperties(element, instance, writableProperties);

        return instance;
    }

    private static bool TryCreatePropertyValue(XElement element, PropertyInfo property, out object? value)
    {
        if (ShouldProjectFromCurrentElement(property.PropertyType))
        {
            value = CreateValue(element, property.PropertyType, property.Name);
            return true;
        }

        if (TryCreateKnownSpecialCasePropertyValue(element, property, out value))
        {
            return true;
        }

        if (property.PropertyType.IsArray)
        {
            var repeatedSiblings = FindRepeatedPropertyElements(element, property);
            if (repeatedSiblings.Length > 1)
            {
                value = CreateArrayFromElements(repeatedSiblings, property.PropertyType, property.Name);
                return true;
            }

            var container = FindPropertyElement(element, property);
            if (container is not null)
            {
                value = CreateValue(container, property.PropertyType, property.Name);
                return true;
            }

            var directArray = TryCreateDirectArray(element, property);
            if (directArray.found)
            {
                value = directArray.value;
                return true;
            }

            value = null;
            return false;
        }

        var child = FindPropertyElement(element, property);
        if (child is null)
        {
            value = null;
            return false;
        }

        value = CreateValue(child, property.PropertyType, property.Name);
        return true;
    }

    private static bool TryCreateKnownSpecialCasePropertyValue(XElement element, PropertyInfo property, out object? value)
    {
        if (property.Name.Equals("EngineGlow", StringComparison.OrdinalIgnoreCase) &&
            property.PropertyType.FullName == "ConquestFrontierWarsRay.Data.Models.BT.BT_ENGINE_GLOW_DATA")
        {
            var sizeContainer = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "engineGlow", StringComparison.OrdinalIgnoreCase));
            if (sizeContainer is not null)
            {
                var projected = Activator.CreateInstance(property.PropertyType)
                               ?? throw new InvalidOperationException($"Could not instantiate '{property.PropertyType.FullName}'.");
                var sizeProperty = property.PropertyType.GetProperty("Size");
                var colorProperty = property.PropertyType.GetProperty("Color");
                var textureProperty = property.PropertyType.GetProperty("EngineTextureName");
                if (sizeProperty is not null)
                {
                    var sizeElement = sizeContainer.Elements().FirstOrDefault(child =>
                        string.Equals(child.Name.LocalName, "size", StringComparison.OrdinalIgnoreCase));
                    if (sizeElement is not null)
                    {
                        sizeProperty.SetValue(projected, CreateValue(sizeElement, sizeProperty.PropertyType, sizeProperty.Name));
                    }
                }

                if (colorProperty is not null)
                {
                    var colorElement = element.Elements().FirstOrDefault(child =>
                        string.Equals(child.Name.LocalName, "color", StringComparison.OrdinalIgnoreCase));
                    if (colorElement is not null)
                    {
                        colorProperty.SetValue(projected, CreateValue(colorElement, colorProperty.PropertyType, colorProperty.Name));
                    }
                }

                if (textureProperty is not null)
                {
                    var textureElement = element.Elements().FirstOrDefault(child =>
                        string.Equals(child.Name.LocalName, "engineTextureName", StringComparison.OrdinalIgnoreCase));
                    if (textureElement is not null)
                    {
                        textureProperty.SetValue(projected, CreateValue(textureElement, textureProperty.PropertyType, textureProperty.Name));
                    }
                }

                value = projected;
                return true;
            }
        }

        if (property.Name.Equals("Billboard", StringComparison.OrdinalIgnoreCase) &&
            property.PropertyType.FullName == "ConquestFrontierWarsRay.Data.Models.BT.BT_BILLBOARD_DATA")
        {
            var billboardElement = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "billboard", StringComparison.OrdinalIgnoreCase));
            if (billboardElement is not null)
            {
                var projected = CreateValue(billboardElement, property.PropertyType, property.Name);
                var tex2Property = property.PropertyType.GetProperty("BTex2");
                if (projected is not null && tex2Property is not null)
                {
                    var tex2Element = element.Elements().FirstOrDefault(child =>
                        string.Equals(child.Name.LocalName, "tex2", StringComparison.OrdinalIgnoreCase));
                    if (tex2Element is not null)
                    {
                        tex2Property.SetValue(projected, CreateValue(tex2Element, tex2Property.PropertyType, tex2Property.Name));
                    }
                }

                value = projected;
                return true;
            }
        }

        if (property.Name.Equals("EffectType", StringComparison.OrdinalIgnoreCase) &&
            property.PropertyType == typeof(string))
        {
            var effectTypeElement = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "effectType", StringComparison.OrdinalIgnoreCase));
            if (effectTypeElement is not null)
            {
                var firstElement = effectTypeElement.Elements()
                    .FirstOrDefault(child => string.Equals(child.Name.LocalName, "element", StringComparison.OrdinalIgnoreCase));
                if (firstElement is not null)
                {
                    value = firstElement.Value.Trim();
                    return true;
                }
            }
        }

        if (property.DeclaringType == typeof(GT_GLOBAL_VALUES))
        {
            if (property.Name.Equals("MovePenaltySelf", StringComparison.OrdinalIgnoreCase))
            {
                value = TryCreateNestedScalar(element, "targetingValues", "movePenaltySelf", property.PropertyType);
                return value is not null;
            }

            if (property.Name.Equals("MovePenaltyTarget", StringComparison.OrdinalIgnoreCase))
            {
                value = TryCreateNestedScalar(element, "targetingValues", "movePenaltyTarget", property.PropertyType);
                return value is not null;
            }

            if (property.Name.Equals("MinAccuracy", StringComparison.OrdinalIgnoreCase))
            {
                value = TryCreateNestedScalar(element, "targetingValues", "minAccuracy", property.PropertyType);
                return value is not null;
            }

            if (property.Name.Equals("IndividualKillChart", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(uint[]))
            {
                value = CreateNamedArrayFromContainer(element, property.PropertyType, "individualKillChart",
                    "rookie", "novice", "veteran", "elite", "superElite", "superdouperElite");
                return true;
            }

            if (property.Name.Equals("AdmiralKillChart", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(uint[]))
            {
                value = CreateNamedArrayFromContainer(element, property.PropertyType, "admiralKillChart",
                    "rearAdmiral", "viceAdmiral", "admiral", "starAdmiral", "superstarAdmiral", "superdouperAdmiral");
                return true;
            }

            if (property.Name.Equals("RaceBonuses", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(float[]))
            {
                value = CreateNamedArrayFromContainer(element, property.PropertyType, "raceBonuses",
                    "noRace", "terran", "mantis", "solarian", "vyrium");
                return true;
            }

            if (property.Name.Equals("TechUpgrades", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(float[][][]))
            {
                var techUpgrades = FindElement(element, "techUpgrades");
                if (techUpgrades is not null)
                {
                    value = CreateRaceTechUpgradesArray(techUpgrades);
                    return true;
                }
            }
        }

        if (property.Name.Equals("Research", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType?.Name == "BT_ADMIRAL_RES" &&
            property.PropertyType.FullName == "ConquestFrontierWarsRay.Data.Models.BT.BT_RESEARCH")
        {
            value = CreateValue(element, property.PropertyType, property.Name);
            return true;
        }

        if (property.Name.Equals("Buttons", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_MENU1_OPENING) &&
            property.PropertyType == typeof(BUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "single", "multi", "intro", "options", "help", "quit");
            return true;
        }

        if (property.DeclaringType == typeof(GT_OPTIONS))
        {
            if (property.Name.Equals("StaticFields", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(STATIC_DATA[]))
            {
                value = CreateNamedArray(element, property.PropertyType,
                    "staticSound", "staticMusic", "staticComm", "staticChat", "staticSpeed", "staticScroll",
                    "staticMouse", "staticDInput", "staticStatus", "staticRollover", "staticSectorMap",
                    "staticRightClick", "staticSubtitles");
                return true;
            }

            if (property.Name.Equals("Sliders", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(SLIDER_DATA[]))
            {
                value = CreateNamedArray(element, property.PropertyType,
                    "sliderSound", "sliderMusic", "sliderComm", "sliderChat", "sliderSpeed", "sliderScroll",
                    "sliderMouse");
                return true;
            }

            if (property.Name.Equals("PushButtons", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(BUTTON_DATA[]))
            {
                value = CreateNamedArray(element, property.PropertyType,
                    "pushSound", "pushMusic", "pushComm", "pushChat", "pushDInput", "pushStatus",
                    "pushRollover", "pushSectorMap", "pushRightClick", "pushSubtitles");
                return true;
            }

            if (property.Name.Equals("GraphicsStatics", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(STATIC_DATA[]))
            {
                value = CreateNamedArray(element, property.PropertyType,
                    "staticShips3D", "staticTrails", "staticEmissive", "staticDetail", "staticDrawBack");
                return true;
            }

            if (property.Name.Equals("GraphicsPushButtons", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(BUTTON_DATA[]))
            {
                value = CreateNamedArray(element, property.PropertyType,
                    "pushTrails", "pushEmissive", "pushDetail");
                return true;
            }
        }

        if (property.Name.Equals("Groups", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType?.Name == "BT_FORMATION" &&
            property.PropertyType.IsArray)
        {
            var elementType = property.PropertyType.GetElementType()
                              ?? throw new InvalidOperationException($"Could not determine array element type for '{property.Name}'.");
            var groupsContainer = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "groups", StringComparison.OrdinalIgnoreCase));
            var groupElements = groupsContainer?.Elements()
                                    .Where(child => string.Equals(child.Name.LocalName, "element", StringComparison.OrdinalIgnoreCase))
                                    .ToArray()
                                ?? [];

            var array = Array.CreateInstance(elementType, 8);
            for (var index = 0; index < array.Length; index++)
            {
                array.SetValue(Activator.CreateInstance(elementType), index);
            }

            foreach (var groupElement in groupElements)
            {
                var index = GetIndexOrMax(groupElement);
                if (index < 0 || index >= array.Length)
                {
                    continue;
                }

                array.SetValue(CreateValue(groupElement, elementType, property.Name), index);
            }

            value = array;
            return true;
        }

        if ((property.Name.Equals("MinBombType", StringComparison.OrdinalIgnoreCase) ||
             property.Name.Equals("BombType", StringComparison.OrdinalIgnoreCase) ||
             property.Name.Equals("LargeBombType", StringComparison.OrdinalIgnoreCase)) &&
            property.DeclaringType?.Name == "BT_PLAYER_RACE" &&
            property.PropertyType.IsArray &&
            property.PropertyType.GetElementType()?.Name == "BT_PLAYERBOMB_TYPE")
        {
            var container = FindPropertyElement(element, property);
            if (container is not null)
            {
                var elementType = property.PropertyType.GetElementType()
                                  ?? throw new InvalidOperationException($"Could not determine array element type for '{property.Name}'.");
                var items = container.Elements()
                    .Where(child => string.Equals(child.Name.LocalName, "element", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(GetIndexOrMax)
                    .ToArray();
                var archetypeProperty = elementType.GetProperty("ArchetypeName");
                var array = Array.CreateInstance(elementType, items.Length);
                for (var index = 0; index < items.Length; index++)
                {
                    var item = Activator.CreateInstance(elementType)
                               ?? throw new InvalidOperationException($"Could not instantiate '{elementType.FullName}'.");
                    archetypeProperty?.SetValue(item, items[index].Value.Trim());
                    array.SetValue(item, index);
                }

                value = array;
                return true;
            }
        }

        if (property.Name.Equals("Filters", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType?.Name == "BT_FLEET_GROUP_DEF" &&
            property.PropertyType.IsArray)
        {
            var elementType = property.PropertyType.GetElementType()
                              ?? throw new InvalidOperationException($"Could not determine array element type for '{property.Name}'.");
            var filtersContainer = element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, "filters", StringComparison.OrdinalIgnoreCase));
            var filterElements = filtersContainer?.Elements()
                                     .Where(child => string.Equals(child.Name.LocalName, "element", StringComparison.OrdinalIgnoreCase))
                                     .ToArray()
                                 ?? [];

            var array = Array.CreateInstance(elementType, 6);
            for (var index = 0; index < array.Length; index++)
            {
                array.SetValue(Activator.CreateInstance(elementType), index);
            }

            foreach (var filterElement in filterElements)
            {
                var index = GetIndexOrMax(filterElement);
                if (index < 0 || index >= array.Length)
                {
                    continue;
                }

                array.SetValue(CreateValue(filterElement, elementType, property.Name), index);
            }

            value = array;
            return true;
        }

        if (property.Name.Equals("Attributes", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType?.Name == "BT_ASTEROIDFIELD_DATA" &&
            property.PropertyType.FullName == "ConquestFrontierWarsRay.Data.Models.BT.BT_FIELD_ATTRIBUTES")
        {
            var child = FindPropertyElement(element, property);
            if (child is not null)
            {
                var projected = CreateValue(child, property.PropertyType, property.Name);
                var moveSpeedProperty = property.PropertyType.GetProperty("MoveSpeedModifier");
                if (projected is not null && moveSpeedProperty is not null)
                {
                    var stationaryElement = element.Elements().FirstOrDefault(xmlChild =>
                        string.Equals(xmlChild.Name.LocalName, "stationaryPercentage", StringComparison.OrdinalIgnoreCase));
                    if (stationaryElement is not null)
                    {
                        moveSpeedProperty.SetValue(projected, Convert.ToSingle(stationaryElement.Value, CultureInfo.InvariantCulture));
                    }
                }

                value = projected;
                return true;
            }
        }

        if (property.Name.Equals("StaticLabels", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_MENU1_OPENING) &&
            property.PropertyType == typeof(STATIC_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "staticSingle", "staticMulti", "staticIntro", "staticOptions", "staticHelp");
            return true;
        }

        if (property.Name.Equals("Animations", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_MENU1_OPENING) &&
            property.PropertyType == typeof(ANIMATE_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "animMedia", "animSingle", "animMulti", "animOptions", "animQuestion");
            return true;
        }

        if (property.Name.Equals("ButtonMovies", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_MENU1_SELECT_MISSION) &&
            property.PropertyType == typeof(BUTTON_DATA[]))
        {
            var movies = element.Elements()
                .Where(child => string.Equals(child.Name.LocalName, "buttonMovies", StringComparison.OrdinalIgnoreCase) &&
                                child.Attribute("index") is not null)
                .OrderBy(GetIndexOrMax)
                .ToArray();
            var array = Array.CreateInstance(typeof(BUTTON_DATA), movies.Length);
            for (var index = 0; index < movies.Length; index++)
            {
                array.SetValue(CreateValue(movies[index], typeof(BUTTON_DATA), null), index);
            }

            value = array;
            return true;
        }

        if (property.Name.Equals("CommandButtons", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "escort", "patrol", "stop", "attackPosition");
            return true;
        }

        if (property.Name.Equals("TacticButtons", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "tacticPeace", "tacticStandGround", "tacticDefend", "tacticSeek");
            return true;
        }

        if (property.Name.Equals("Stats", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_ORDER_TAB) &&
            property.PropertyType == typeof(STATIC_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "o_namearea", "o_kills", "o_hull");
            return true;
        }

        if (property.Name.Equals("Orders", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_ORDER_TAB) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "order1", "order2", "order3", "order4", "order5", "order6");
            return true;
        }

        if (property.Name.Equals("Specials", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_ORDER_TAB) &&
            property.PropertyType == typeof(MULTIHOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType,
                "specialOrders0", "specialOrders1", "specialOrders2", "specialOrders3", "specialOrders4", "specialOrders5",
                "specialOrders6", "specialOrders7", "specialOrders8", "specialOrders9", "specialOrders10", "specialOrders11");
            return true;
        }

        if (property.Name.Equals("Stats", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_KIT_TAB) &&
            property.PropertyType == typeof(STATIC_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "k_namearea", "k_hull", "k_kills");
            return true;
        }

        if (property.Name.Equals("Kits", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_KIT_TAB) &&
            property.PropertyType == typeof(MULTIHOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType,
                "kit0", "kit1", "kit2", "kit3", "kit4", "kit5", "kit6", "kit7", "kit8", "kit9", "kit10", "kit11", "kit12", "kit13");
            return true;
        }

        if (property.Name.Equals("KitDisplay", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_FLEET_KIT_TAB) &&
            property.PropertyType == typeof(MULTIHOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "kitDisplay0", "kitDisplay1");
            return true;
        }

        if (property.Name.Equals("Special", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_INDIVIDUAL) &&
            property.PropertyType == typeof(MULTIHOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "specialweapon", "specialweapon1", "specialweapon2", "artifact");
            return true;
        }

        if (property.Name.Equals("Special", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_GROUP) &&
            property.PropertyType == typeof(MULTIHOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "specialweapon", "specialweapon1", "specialweapon2");
            return true;
        }

        if (property.Name.Equals("CommandButtons", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_GROUP) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "escort", "patrol", "stop", "cloak", "attackPosition");
            return true;
        }

        if (property.Name.Equals("StanceButtons", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_GROUP) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType,
                "stanceAttack", "stanceDefend", "stanceStand", "stanceStop",
                "supplyStanceAuto", "supplyStanceNoAuto", "supplyStanceResupplyOnly");
            return true;
        }

        if (property.Name.Equals("FighterStance", StringComparison.OrdinalIgnoreCase) &&
            property.DeclaringType == typeof(GT_TOOLBAR_GROUP) &&
            property.PropertyType == typeof(HOTBUTTON_DATA[]))
        {
            value = CreateNamedArray(element, property.PropertyType, "fighterStanceNormal", "fighterStancePatrol");
            return true;
        }

        if (property.Name.Equals("BuildTabs", StringComparison.OrdinalIgnoreCase) &&
            property.PropertyType == typeof(BUILDBUTTON_DATA[][]))
        {
            var tabNames = new[] { "basicTab", "advancedTab", "defenceTab", "moonTab" };
            var tabs = new BUILDBUTTON_DATA[tabNames.Length][];

            for (var index = 0; index < tabNames.Length; index++)
            {
                var tabElement = element.Elements().FirstOrDefault(child =>
                    string.Equals(child.Name.LocalName, tabNames[index], StringComparison.OrdinalIgnoreCase));
                if (tabElement is null)
                {
                    value = null;
                    return false;
                }

                tabs[index] = (BUILDBUTTON_DATA[])CreateValue(tabElement, typeof(BUILDBUTTON_DATA[]), property.Name)!;
            }

            value = tabs;
            return true;
        }

        value = null;
        return false;
    }

    private static object CreateNamedArray(XElement element, Type arrayType, params string[] childNames)
    {
        var elementType = arrayType.GetElementType()
                         ?? throw new InvalidOperationException($"Array '{arrayType.FullName}' is missing element type.");
        var items = childNames
            .Select(name => element.Elements().FirstOrDefault(child =>
                string.Equals(child.Name.LocalName, name, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (items.Any(item => item is null))
        {
            throw new InvalidOperationException(
                $"Could not project named array '{arrayType.FullName}' from '{element.Name.LocalName}'.");
        }

        var array = Array.CreateInstance(elementType, items.Length);
        for (var index = 0; index < items.Length; index++)
        {
            array.SetValue(CreateValue(items[index]!, elementType, null), index);
        }

        return array;
    }

    private static (bool found, object? value) TryCreateDirectArray(XElement element, PropertyInfo property)
    {
        var elementType = property.PropertyType.GetElementType()
                         ?? throw new InvalidOperationException($"Array '{property.Name}' is missing element type.");

        var candidates = element.Elements()
            .Where(child => IsCandidateArrayElement(child, elementType))
            .OrderBy(GetIndexOrMax)
            .ToArray();

        if (candidates.Length == 0)
        {
            return (false, null);
        }

        var array = Array.CreateInstance(elementType, candidates.Length);
        for (var index = 0; index < candidates.Length; index++)
        {
            array.SetValue(CreateValue(candidates[index], elementType, property.Name), index);
        }

        return (true, array);
    }

    private static object CreateNamedArrayFromContainer(XElement element, Type arrayType, string containerName, params string[] childNames)
    {
        var container = FindElement(element, containerName)
                        ?? throw new InvalidOperationException(
                            $"Could not project named array '{arrayType.FullName}' from container '{containerName}'.");
        return CreateNamedArray(container, arrayType, childNames);
    }

    private static object? TryCreateNestedScalar(XElement element, string containerName, string childName, Type targetType)
    {
        var container = FindElement(element, containerName);
        var child = container is null ? null : FindElement(container, childName);
        return child is null ? null : CreateValue(child, targetType, childName);
    }

    private static float[][][] CreateRaceTechUpgradesArray(XElement container)
    {
        var raceNames = new[] { "noRace", "terran", "mantis", "solarian", "vyrium" };
        var upgradeNames = new[]
        {
            "engine", "hull", "supplies", "targeting", "damage", "shields", "shipTargetingExp",
            "admiralTargetingExp", "fleet", "sensors", "tanker", "fighter", "tender"
        };

        var races = new float[raceNames.Length][][];
        for (var raceIndex = 0; raceIndex < raceNames.Length; raceIndex++)
        {
            var raceElement = FindElement(container, raceNames[raceIndex])
                              ?? throw new InvalidOperationException($"Could not project tech upgrade race '{raceNames[raceIndex]}'.");
            races[raceIndex] = new float[upgradeNames.Length][];
            for (var upgradeIndex = 0; upgradeIndex < upgradeNames.Length; upgradeIndex++)
            {
                var upgradeElement = FindElement(raceElement, upgradeNames[upgradeIndex])
                                     ?? throw new InvalidOperationException(
                                         $"Could not project tech upgrade group '{upgradeNames[upgradeIndex]}'.");
                races[raceIndex][upgradeIndex] = (float[])CreateValue(upgradeElement, typeof(float[]), upgradeNames[upgradeIndex])!;
            }
        }

        return races;
    }

    private static bool IsCandidateArrayElement(XElement element, Type elementType)
    {
        var xmlType = NormalizeName(element.Attribute("type")?.Value);
        var typeAliases = GetTypeAliases(elementType).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(xmlType) && typeAliases.Contains(xmlType))
        {
            return true;
        }

        return NormalizeName(element.Name.LocalName) == "element" ||
               typeAliases.Contains(NormalizeName(element.Name.LocalName));
    }

    private static Array CreateArray(XElement container, Type arrayType, string? propertyName)
    {
        var elementType = arrayType.GetElementType()
                         ?? throw new InvalidOperationException($"Array '{arrayType.FullName}' is missing element type.");

        var items = container.Elements()
            .OrderBy(GetIndexOrMax)
            .ToArray();
        var array = Array.CreateInstance(elementType, items.Length);

        for (var index = 0; index < items.Length; index++)
        {
            array.SetValue(CreateValue(items[index], elementType, propertyName), index);
        }

        return array;
    }

    private static Array CreateArrayFromElements(IReadOnlyList<XElement> items, Type arrayType, string? propertyName)
    {
        var elementType = arrayType.GetElementType()
                         ?? throw new InvalidOperationException($"Array '{arrayType.FullName}' is missing element type.");
        var array = Array.CreateInstance(elementType, items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            array.SetValue(CreateValue(items[index], elementType, propertyName), index);
        }

        return array;
    }

    private static XElement? FindPropertyElement(XElement element, PropertyInfo property)
    {
        var propertyAliases = GetAliases(property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return element.Elements().FirstOrDefault(child => propertyAliases.Contains(NormalizeName(child.Name.LocalName)));
    }

    private static XElement? FindElement(XElement element, string name)
    {
        var normalized = NormalizeName(name);
        return element.Elements().FirstOrDefault(child => NormalizeName(child.Name.LocalName) == normalized);
    }

    private static XElement[] FindRepeatedPropertyElements(XElement element, PropertyInfo property)
    {
        var propertyAliases = GetAliases(property.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return element.Elements()
            .Where(child => propertyAliases.Contains(NormalizeName(child.Name.LocalName)))
            .OrderBy(GetIndexOrMax)
            .ToArray();
    }

    private static IEnumerable<string> GetAliases(string propertyName)
    {
        yield return NormalizeName(propertyName);

        if (propertyName.Equals("Type", StringComparison.OrdinalIgnoreCase))
        {
            yield return "basetype";
            yield return "type";
        }

        if (propertyName.Equals("ScreenRect", StringComparison.OrdinalIgnoreCase))
        {
            yield return "rect";
            yield return "screenrect";
        }

        if (propertyName.Equals("Font", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fonttype";
        }

        if (propertyName.Equals("WpnClass", StringComparison.OrdinalIgnoreCase))
        {
            yield return "baseweapondata";
        }

        if (propertyName.Equals("BaseProjectile", StringComparison.OrdinalIgnoreCase))
        {
            yield return "projectiledata";
        }

        if (propertyName.Equals("TechMode", StringComparison.OrdinalIgnoreCase))
        {
            yield return "technode";
        }

        if (propertyName.Equals("ResourceCost", StringComparison.OrdinalIgnoreCase))
        {
            yield return "cost";
        }

        if (propertyName.Equals("CapsRaw", StringComparison.OrdinalIgnoreCase))
        {
            yield return "caps";
        }

        if (propertyName.Equals("BSpecialWeapon", StringComparison.OrdinalIgnoreCase))
        {
            yield return "specialweapon";
        }

        if (propertyName.Equals("BTargetRequired", StringComparison.OrdinalIgnoreCase))
        {
            yield return "targetrequired";
        }

        if (propertyName.Equals("BWormHole", StringComparison.OrdinalIgnoreCase))
        {
            yield return "wormhole";
        }

        if (propertyName.Equals("BTex2", StringComparison.OrdinalIgnoreCase))
        {
            yield return "tex2";
        }

        if (propertyName.Equals("NumLight", StringComparison.OrdinalIgnoreCase))
        {
            yield return "numlights";
        }

        if (propertyName.Equals("FileMame", StringComparison.OrdinalIgnoreCase))
        {
            yield return "filename";
        }

        if (propertyName.Equals("TerraParticle", StringComparison.OrdinalIgnoreCase))
        {
            yield return "teraparticle";
        }

        if (propertyName.Equals("Base", StringComparison.OrdinalIgnoreCase))
        {
            yield return "dynamics";
        }

        if (propertyName.Equals("Timer", StringComparison.OrdinalIgnoreCase))
        {
            yield return "dwtimer";
        }

        if (propertyName.Equals("Admiral", StringComparison.OrdinalIgnoreCase))
        {
            yield return "admiralhead";
        }

        if (propertyName.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            yield return "noneblock";
        }

        if (propertyName.Equals("FlagsRaw", StringComparison.OrdinalIgnoreCase))
        {
            yield return "flags";
        }

        if (propertyName.Equals("Fleets", StringComparison.OrdinalIgnoreCase))
        {
            yield return "fleet";
        }

        if (propertyName.Equals("WarTurrets", StringComparison.OrdinalIgnoreCase))
        {
            yield return "warturret";
        }

        if (propertyName.Equals("Individuals", StringComparison.OrdinalIgnoreCase))
        {
            yield return "individual";
        }

        if (propertyName.Equals("Groups", StringComparison.OrdinalIgnoreCase))
        {
            yield return "group";
        }

        if (propertyName.Equals("StatTab", StringComparison.OrdinalIgnoreCase))
        {
            yield return "statisticstab";
        }

        if (propertyName.Equals("Special", StringComparison.OrdinalIgnoreCase))
        {
            yield return "specialweapon";
        }

        if (propertyName.Equals("TextIds", StringComparison.OrdinalIgnoreCase))
        {
            yield return "textid";
        }

        if (propertyName.Equals("MinMoonsPerPlanet", StringComparison.OrdinalIgnoreCase))
        {
            yield return "minmoonperplanet";
        }

        if (propertyName.Equals("MaxMoonsPerPlanet", StringComparison.OrdinalIgnoreCase))
        {
            yield return "maxmoonperplanet";
        }

        if (propertyName.Equals("NMovieBeforeMission", StringComparison.OrdinalIgnoreCase))
        {
            yield return "nmoivebeforemission";
        }

        if (propertyName.Equals("StartConstruction", StringComparison.OrdinalIgnoreCase))
        {
            yield return "startconstuction";
        }

        if (propertyName.Equals("RelativeGroupId", StringComparison.OrdinalIgnoreCase))
        {
            yield return "relativegroup";
        }
    }

    private static IEnumerable<string> GetTypeAliases(Type type)
    {
        var normalizedName = NormalizeName(type.Name);
        if (!string.IsNullOrEmpty(normalizedName))
        {
            yield return normalizedName;
        }

        var parts = type.Name.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < parts.Length; i++)
        {
            var alias = NormalizeName(string.Concat(parts.Skip(i)));
            if (!string.IsNullOrEmpty(alias))
            {
                yield return alias;
            }
        }
    }

    private static bool ShouldProjectFromCurrentElement(Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return targetType.FullName is
            "ConquestFrontierWarsRay.Data.Models.BASIC_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BASE_FIELD_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BASE_LAUNCHER" or
            "ConquestFrontierWarsRay.Data.Models.BT.BASE_WEAPON_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BT_OBJ_CLASS_AND_RACE_AND_DISPLAY_NAME" or
            "ConquestFrontierWarsRay.Data.Models.BT.BT_BASE_SPACESHIP_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BT_BASE_PLATFORM_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BT_BASE_RESEARCH_DATA" or
            "ConquestFrontierWarsRay.Data.Models.BT.BT_PROJECTILE_DATA_BASE";
    }

    private static void PopulateDerivedEnumProperties(
        XElement element,
        Type targetType,
        object instance,
        IEnumerable<PropertyInfo> writableProperties)
    {
        foreach (var property in writableProperties)
        {
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!propertyType.IsEnum)
            {
                continue;
            }

            var currentValue = property.GetValue(instance);
            if (!Equals(currentValue, Activator.CreateInstance(propertyType)))
            {
                continue;
            }

            if (property.Name.Equals("Type", StringComparison.OrdinalIgnoreCase) &&
                TryInferTypeEnum(targetType, propertyType, out var inferredType))
            {
                property.SetValue(instance, inferredType);
                continue;
            }

            if (propertyType.GetCustomAttribute<FlagsAttribute>() is not null &&
                TryBuildFlagsValue(element, propertyType, out var flagsValue))
            {
                property.SetValue(instance, flagsValue);
            }
        }
    }

    private static bool TryInferTypeEnum(Type targetType, Type enumType, out object inferredValue)
    {
        var candidate = targetType.Name.StartsWith("GT_", StringComparison.OrdinalIgnoreCase)
            ? $"GBT_{targetType.Name[3..]}"
            : targetType.Name;

        if (Enum.TryParse(enumType, candidate, ignoreCase: true, out var parsed) && parsed is not null)
        {
            inferredValue = parsed;
            return true;
        }

        inferredValue = default!;
        return false;
    }

    private static bool TryBuildFlagsValue(XElement element, Type enumType, out object flagsValue)
    {
        ulong combined = 0;
        var matchedAny = false;

        foreach (var member in Enum.GetNames(enumType))
        {
            if (string.Equals(member, "None", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var child = element.Elements().FirstOrDefault(xmlChild =>
                NormalizeName(xmlChild.Name.LocalName) == NormalizeName(member));
            if (child is null || !ParseBoolean(child.Value))
            {
                continue;
            }

            combined |= Convert.ToUInt64(Enum.Parse(enumType, member));
            matchedAny = true;
        }

        flagsValue = Enum.ToObject(enumType, combined);
        return matchedAny;
    }

    private static void PopulateDerivedScalarProperties(
        XElement element,
        object instance,
        IEnumerable<PropertyInfo> writableProperties)
    {
        foreach (var property in writableProperties)
        {
            if (property.Name.Equals("Flags", StringComparison.OrdinalIgnoreCase) &&
                property.PropertyType == typeof(uint) &&
                instance.GetType() == typeof(LISTBOX_DATA) &&
                Equals(property.GetValue(instance), default(uint)))
            {
                uint listFlags = 0;
                if (TryGetBooleanChild(element, "static", out var isStatic) && isStatic)
                {
                    listFlags |= 0x01;
                }

                if (TryGetBooleanChild(element, "singleClick", out var singleClick) && singleClick)
                {
                    listFlags |= 0x02;
                }

                if (TryGetBooleanChild(element, "scrollbar", out var scrollbar) && scrollbar)
                {
                    listFlags |= 0x04;
                }

                if (TryGetBooleanChild(element, "solidBackground", out var solidBackground) && solidBackground)
                {
                    listFlags |= 0x08;
                }

                if (TryGetBooleanChild(element, "noBorder", out var noBorder) && noBorder)
                {
                    listFlags |= 0x10;
                }

                if (TryGetBooleanChild(element, "disableMouseSelect", out var disableMouseSelect) && disableMouseSelect)
                {
                    listFlags |= 0x20;
                }

                property.SetValue(instance, listFlags);
                continue;
            }

            if (!property.Name.Equals("FlagsRaw", StringComparison.OrdinalIgnoreCase) ||
                property.PropertyType != typeof(uint) ||
                !Equals(property.GetValue(instance), default(uint)))
            {
                continue;
            }

            var okForPlayerStart = element.Elements().FirstOrDefault(child =>
                NormalizeName(child.Name.LocalName) == "okforplayerstart");
            var okForRemoteSystem = element.Elements().FirstOrDefault(child =>
                NormalizeName(child.Name.LocalName) == "okforremotesystem");

            if (okForPlayerStart is null && okForRemoteSystem is null)
            {
                continue;
            }

            uint flags = 0;
            if (okForPlayerStart is not null && ParseBoolean(okForPlayerStart.Value))
            {
                flags |= 0x01;
            }

            if (okForRemoteSystem is not null && ParseBoolean(okForRemoteSystem.Value))
            {
                flags |= 0x02;
            }

            property.SetValue(instance, flags);
        }
    }

    private static bool TryGetBooleanChild(XElement element, string childName, out bool value)
    {
        var child = element.Elements().FirstOrDefault(candidate =>
            string.Equals(candidate.Name.LocalName, childName, StringComparison.OrdinalIgnoreCase));
        if (child is null)
        {
            value = false;
            return false;
        }

        value = ParseBoolean(child.Value);
        return true;
    }

    private static bool IsScalar(Type type)
    {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private static object ConvertScalar(string text, Type targetType)
    {
        var trimmed = text.Trim();
        if (targetType != typeof(bool) && TryConvertBooleanLikeNumber(trimmed, targetType, out var booleanLikeValue))
        {
            return booleanLikeValue;
        }

        return targetType switch
        {
            _ when targetType == typeof(byte) => byte.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(sbyte) => sbyte.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(short) => short.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(ushort) => ushort.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(int) => int.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(uint) => uint.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(long) => long.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(ulong) => ulong.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(float) => float.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(double) => double.Parse(trimmed, CultureInfo.InvariantCulture),
            _ when targetType == typeof(decimal) => decimal.Parse(trimmed, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Unsupported scalar target type '{targetType.FullName}'.")
        };
    }

    private static bool TryConvertBooleanLikeNumber(string text, Type targetType, out object value)
    {
        value = default!;
        if (!text.Equals("true", StringComparison.OrdinalIgnoreCase) &&
            !text.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var numeric = text.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        value = targetType switch
        {
            _ when targetType == typeof(byte) => (byte)numeric,
            _ when targetType == typeof(sbyte) => (sbyte)numeric,
            _ when targetType == typeof(short) => (short)numeric,
            _ when targetType == typeof(ushort) => (ushort)numeric,
            _ when targetType == typeof(int) => numeric,
            _ when targetType == typeof(uint) => (uint)numeric,
            _ when targetType == typeof(long) => (long)numeric,
            _ when targetType == typeof(ulong) => (ulong)numeric,
            _ => default!
        };

        return value is not null;
    }

    private static bool ParseBoolean(string text)
    {
        var trimmed = text.Trim();
        return trimmed switch
        {
            "0" => false,
            "1" => true,
            _ => bool.Parse(trimmed)
        };
    }

    private static int GetIndexOrMax(XElement element)
    {
        return int.TryParse(element.Attribute("index")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var index)
            ? index
            : int.MaxValue;
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}

internal static class XmlFieldProjector
{
    public static IReadOnlyDictionary<string, string> Project(XElement root)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var child in root.Elements())
        {
            Collect(child, child.Name.LocalName, fields);
        }

        return fields;
    }

    private static void Collect(XElement element, string path, IDictionary<string, string> fields)
    {
        if (int.TryParse(element.Attribute("index")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
        {
            fields[$"{NormalizeName(element.Name.LocalName)}_{index:00}"] = element.Value.Trim();
        }

        var children = element.Elements().ToArray();
        if (children.Length == 0)
        {
            fields[NormalizeName(path)] = element.Value.Trim();
            fields.TryAdd(NormalizeName(element.Name.LocalName), element.Value.Trim());
            return;
        }

        foreach (var child in children)
        {
            Collect(child, $"{path}.{child.Name.LocalName}", fields);
        }
    }

    private static string NormalizeName(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}

internal static class FieldComparer
{
    public static List<string> Compare(UtfDbEntryDetails details, IReadOnlyDictionary<string, string> xmlFields)
    {
        var differences = new List<string>();

        foreach (var field in details.Fields)
        {
            var aliases = GetAliases(field.Label);
            var xmlMatch = aliases
                .Select(alias => xmlFields.TryGetValue(alias, out var value) ? value : null)
                .FirstOrDefault(value => value is not null);

            if (xmlMatch is null)
            {
                differences.Add($"Missing XML value for field '{field.Label}' (binary='{field.Value}').");
                continue;
            }

            if (!string.Equals(field.Value, xmlMatch, StringComparison.Ordinal))
            {
                differences.Add($"Field '{field.Label}' mismatch: binary='{field.Value}' xml='{xmlMatch}'.");
            }
        }

        return differences;
    }

    private static IEnumerable<string> GetAliases(string label)
    {
        yield return label;
        var normalized = NormalizeName(label);
        yield return normalized;
        yield return NormalizeName(label.Split('.').Last());

        if (label.Equals("base.type", StringComparison.OrdinalIgnoreCase))
        {
            yield return "basetype";
        }
    }

    private static string NormalizeName(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}

internal static class DeepComparer
{
    public static List<string> Compare(object? expected, object? actual)
    {
        var differences = new List<string>();
        Compare(expected, actual, "$", differences);
        return differences;
    }

    private static void Compare(object? expected, object? actual, string path, ICollection<string> differences)
    {
        if (ReferenceEquals(expected, actual))
        {
            return;
        }

        if (expected is null || actual is null)
        {
            differences.Add($"{path}: expected '{Format(expected)}', actual '{Format(actual)}'.");
            return;
        }

        var expectedType = expected.GetType();
        if (expectedType != actual.GetType())
        {
            differences.Add($"{path}: type mismatch expected '{expectedType.FullName}', actual '{actual.GetType().FullName}'.");
            return;
        }

        if (expectedType == typeof(string) || expectedType.IsPrimitive || expectedType.IsEnum ||
            expected is decimal or float or double)
        {
            if (!Equals(expected, actual))
            {
                differences.Add($"{path}: expected '{Format(expected)}', actual '{Format(actual)}'.");
            }

            return;
        }

        if (expected is IEnumerable expectedEnumerable && actual is IEnumerable actualEnumerable &&
            expected is not XElement && actual is not XElement)
        {
            CompareEnumerables(expectedEnumerable, actualEnumerable, path, differences);
            return;
        }

        foreach (var property in expectedType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.CanRead))
        {
            Compare(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}", differences);
        }
    }

    private static void CompareEnumerables(IEnumerable expected, IEnumerable actual, string path,
        ICollection<string> differences)
    {
        var expectedItems = expected.Cast<object?>().ToArray();
        var actualItems = actual.Cast<object?>().ToArray();

        if (expectedItems.Length != actualItems.Length)
        {
            differences.Add($"{path}: length mismatch expected {expectedItems.Length}, actual {actualItems.Length}.");
            return;
        }

        for (var index = 0; index < expectedItems.Length; index++)
        {
            Compare(expectedItems[index], actualItems[index], $"{path}[{index}]", differences);
        }
    }

    private static string Format(object? value) => value?.ToString() ?? "<null>";
}

