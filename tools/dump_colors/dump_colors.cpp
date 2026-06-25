#include <vtkNamedColors.h>
#include <vtkSmartPointer.h>
#include <vtkColor.h>
#include <iostream>
#include <string>
#include <sstream>
#include <algorithm>
#include <vector>
#include <cmath>
#include <cctype>
#include <unordered_set>

// Atomic (non-compound) words for splitting CSS3 run-together color names.
// Each word must NOT be further decomposable. Longest-first for greedy matching.
static const std::vector<std::string> Words = {
    // Longer atomic color nouns that are multi-syllable but don't decompose
    "goldenrod", "turquoise", "lavender", "aquamarine", "chartreuse",
    "gainsboro", "burlywood", "cornsilk", "chocolate",
    // Modifiers (adjectives)
    "dark", "light", "medium", "deep", "pale",
    "hot", "cold", "warm", "raw", "burnt",
    // Noun modifiers (things that precede a color word)
    "cornflower", "midnight", "cadet", "royal", "dodger", "powder",
    "indian", "rosy", "sandy", "saddle", "forest", "spring",
    "floral", "ghost", "misty", "blanched", "antique", "alice",
    "navajo", "lemon", "peach", "papaya", "mint",
    "bisque", "orchid", "salmon", "coral", "khaki", "peru", "plum",
    "sienna", "thistle", "azure", "ivory", "beige", "linen",
    "seashell", "honeydew", "moccasin",
    "firebrick", "olivedrab", "whitesmoke", "mintcream",
    // Additional atomic nouns used in color names
    "brick", "melon", "sepia", "cerulean", "cobalt",
    "carrot", "banana", "peacock", "cream",
    "raspberry", "ochre", "umber", "sap", "madder",
    "crimson", "fuchsia", "indigo", "maroon",
    "emerald", "sapphire", "violet", "purple",
    "slate", "steel", "sky", "sea", "lawn", "lime",
    "gold", "gray", "grey", "pink", "tan", "teal",
    "aqua", "navy", "olive", "peru", "plum", "wheat",
    "tomato", "coral",
    // Basic short color words (last, so longer atomic words win)
    "white", "black", "brown",
    "red", "green", "blue", "cyan", "magenta", "yellow", "orange",
    "silver", "gold",
};

// Insert underscores into a run-together lowercase string.
// Greedy: at each position, find the longest matching word in Words.
static std::string InsertUnderscores(const std::string& s) {
    std::string result;
    size_t i = 0;
    while (i < s.size()) {
        size_t bestLen = 0;
        for (const auto& word : Words) {
            if (word.size() > s.size() - i) continue;
            if (word.size() <= bestLen) continue; // only interested in longer matches
            if (s.compare(i, word.size(), word) == 0) {
                bestLen = word.size();
            }
        }
        if (bestLen > 0) {
            if (!result.empty()) result += '_';
            result += s.substr(i, bestLen);
            i += bestLen;
        } else {
            result += s[i];
            i++;
        }
    }
    return result;
}

static std::string ToPascalCase(const std::string& name) {
    std::string snake = name;
    if (snake.find('_') == std::string::npos) {
        snake = InsertUnderscores(snake);
    }
    std::string result;
    bool nextUpper = true;
    for (char c : snake) {
        if (c == '_' || c == ' ') {
            nextUpper = true;
        } else {
            result += nextUpper ? (char)toupper(c) : c;
            nextUpper = false;
        }
    }
    return result;
}

static std::string FmtDouble(double v) {
    if (v == 0.0) return "0";
    if (v == 1.0) return "1";
    char b[64];
    snprintf(b, sizeof(b), "%.6g", v);
    return b;
}

static bool SameColor(const vtkColor3d& a, const vtkColor3d& b) {
    return std::abs(a[0]-b[0]) < 1e-4 && std::abs(a[1]-b[1]) < 1e-4 && std::abs(a[2]-b[2]) < 1e-4;
}

// Words that are good terminal words in a color constant name (i.e. the name should end with a color noun, not a modifier).
static const std::unordered_set<std::string> ColorNouns = {
    // Basic CSS colors
    "White","Black","Brown","Ivory","Beige","Cream",
    "Red","Green","Blue","Cyan","Magenta","Yellow","Orange",
    "Pink","Purple","Violet","Indigo","Gray","Grey",
    "Gold","Silver","Coral","Teal","Navy","Maroon","Olive",
    "Aqua","Fuchsia","Lime","Khaki","Peru","Plum",
    "Sienna","Tan","Thistle","Azure","Orchid",
    "Salmon","Tomato","Chocolate","Wheat",
    // Extended color nouns
    "Goldenrod","Turquoise","Aquamarine","Chartreuse","Bisque",
    "Moccasin","Linen","Gainsboro","Crimson","Indigo",
    "Brick","Melon","Sepia","Cerulean","Cobalt",
    "Cadet","Carrot","Banana","Peacock","Mint",
    "Seashell","Honeydew","Lavender","Cornsilk","Burlywood",
    "Raspberry","Ochre","Umber","Sap","SapGreen",
    "Violet","VioletRed","SlateBlue","SlateGray","SlateGrey",
    "SeaGreen","SkyBlue","SpringGreen","SteelBlue","SteelBlue",
    "Firebrick","OliveDrab","OrangeRed","CadetBlue","RoyalBlue",
    "DodgerBlue","PowderBlue","MidnightBlue","ForestGreen",
    "DeepPink","HotPink","IndianRed","LimeGreen","LawnGreen",
};

// Return the last PascalCase word segment of a PascalCase identifier.
// e.g. "DarkBlue" -> "Blue", "SeaGreenDark" -> "Dark", "GoldenrodDark" -> "Dark"
static std::string LastPascalWord(const std::string& pascal) {
    size_t lastUpper = 0;
    for (size_t i = 1; i < pascal.size(); i++) {
        if (isupper((unsigned char)pascal[i])) lastUpper = i;
    }
    return pascal.substr(lastUpper);
}

// Modifiers that conventionally appear first in C#/CSS color naming (adjective + noun)
static const std::unordered_set<std::string> LeadingModifiers = {
    "Dark","Light","Medium","Deep","Pale","Hot","Cold","Warm",
};

// Score: prefer names ending in a basic color word (CSS convention: DarkBlue > BlueDark).
// Tie-break 1: prefer names starting with a known modifier (adjective-first form).
// Tie-break 2: snake_case names are cleaner. Tie-break 3: longer = more specific.
static int NameScore(const std::string& origName, const std::string& pascal) {
    int score = 0;
    std::string last = LastPascalWord(pascal);
    if (ColorNouns.count(last)) score += 1000;   // strong preference for ...+ColorNoun form
    // First PascalCase word
    size_t firstLower = pascal.size();
    for (size_t i = 1; i < pascal.size(); i++) {
        if (islower((unsigned char)pascal[i])) { firstLower = i; break; }
    }
    std::string first = pascal.substr(0, firstLower);
    if (LeadingModifiers.count(first)) score += 50;
    if (origName.find('_') != std::string::npos) score += 10;
    score += (int)origName.size();
    return score;
}

int main() {
    vtkSmartPointer<vtkNamedColors> colors = vtkSmartPointer<vtkNamedColors>::New();
    std::string namesStr = colors->GetColorNames();

    struct Entry { std::string origName; vtkColor3d color; std::string pascal; int score; };
    std::vector<Entry> entries;

    std::stringstream ss(namesStr);
    std::string name;
    while (std::getline(ss, name, '\n')) {
        while (!name.empty() && (name.back() == '\r' || name.back() == ' ')) name.pop_back();
        size_t start = 0;
        while (start < name.size() && name[start] == ' ') start++;
        name = name.substr(start);
        if (name.empty()) continue;
        vtkColor3d c = colors->GetColor3d(name);
        entries.push_back({name, c, ToPascalCase(name), NameScore(name, ToPascalCase(name))});
    }

    std::sort(entries.begin(), entries.end(), [](const Entry& a, const Entry& b) {
        if (a.score != b.score) return a.score > b.score;
        return a.pascal < b.pascal;
    });

    // Deduplicate by PascalCase name first, then by color value.
    std::vector<Entry> unique;
    std::unordered_set<std::string> seenNames;
    for (const auto& e : entries) {
        if (seenNames.count(e.pascal)) continue;
        bool dupColor = false;
        for (const auto& u : unique) {
            if (SameColor(u.color, e.color)) { dupColor = true; break; }
        }
        if (dupColor) continue;
        seenNames.insert(e.pascal);
        unique.push_back(e);
    }

    std::sort(unique.begin(), unique.end(), [](const Entry& a, const Entry& b) { return a.pascal < b.pascal; });

    std::cout << "    // Color constants from vtkNamedColors (VTK legacy + CSS3 web colors)\n";
    std::cout << "    // Auto-generated by tools/dump_colors — do not edit manually.\n";
    for (const auto& e : unique) {
        std::cout << "    public static readonly VtkSharpColor3d " << e.pascal
                  << " = new(" << FmtDouble(e.color[0]) << ", "
                  << FmtDouble(e.color[1]) << ", " << FmtDouble(e.color[2]) << ");\n";
    }
    std::cerr << "Total unique colors: " << unique.size() << "\n";
    return 0;
}
