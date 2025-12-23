// building-height-and-footprint-component.tsx
import React from "react";
import { bindValue, useValue } from "cs2/api";
import { SelectedInfoSectionBase } from "cs2/bindings";
import { ModuleResolver } from "mods/module-resolver";
import mod from "../../mod.json";

// Bind to C# values
const statsText$ = bindValue<string>(
    "BuildingHeightAndFootprint",
    "statsText",
    "" // default: no stats
);

const layoutKind$ = bindValue<string>(
    "BuildingHeightAndFootprint",
    "layoutKind",
    "" // "Level" / "Description" / ""
);

interface BuildingHeightAndFootprintSectionProps extends SelectedInfoSectionBase {}

// Small helper so logs don't explode
function safeJson(value: any, maxLen: number = 600): string {
    try {
        const json = JSON.stringify(value);
        if (!json) return String(value);
        return json.length > maxLen ? json.slice(0, maxLen) + "..." : json;
    } catch {
        return String(value);
    }
}

// This is the function that `moduleRegistry.extend` expects.
export const BuildingHeightAndFootprintComponent = (componentList: any): any => {
    let keys: string[] = [];
    try {
        keys = componentList ? Object.keys(componentList) : [];
    } catch {
        // ignore
    }

    if (!componentList || typeof componentList !== "object") {
        return componentList;
    }

    const { InfoSection, InfoRow, FoldoutPanel } = ModuleResolver.instance;

    if (!InfoSection || !InfoRow || !FoldoutPanel) {
        return componentList;
    }

    // We only care about two well-known sections:
    //  - DescriptionSection → ploppables / services / uniques
    //  - LevelSection       → zonables
    const candidateKeys = [
        "Game.UI.InGame.DescriptionSection",
        "Game.UI.InGame.LevelSection",
    ];

    const makeWrapper =
        (originalSection: any, targetKey: string) =>
        (props: BuildingHeightAndFootprintSectionProps) => {

            const originalElement = originalSection(props);

            const statsText = useValue(statsText$);
            const layoutKind = useValue(layoutKind$) || "";
            const statsPreview =
                typeof statsText === "string"
                    ? statsText.slice(0, 120)
                    : statsText;

            // Decide if THIS section is the one that should show the stats,
            // based on the layoutKind from C#.
            const isDescriptionSection = targetKey === "Game.UI.InGame.DescriptionSection";
            const isLevelSection = targetKey === "Game.UI.InGame.LevelSection";

            let shouldAttach = false;
            if (layoutKind === "Description" && isDescriptionSection) {
                shouldAttach = true;
            } else if (layoutKind === "Level" && isLevelSection) {
                shouldAttach = true;
            }

            // If layoutKind doesn't match this section, or no stats text, just render original.
            if (
                !shouldAttach ||
                !statsText ||
                (typeof statsText === "string" && statsText.trim().length === 0)
            ) {
                return originalElement;
            }

            // Parse the C# text:
            //   Height: 58.1 ft
            //   Estimated floors: 4
            const lines =
                typeof statsText === "string"
                    ? statsText.trim().split(/\r?\n+/)
                    : [];

            const parsedRows = lines
                .map((line) => {
                    const idx = line.indexOf(":");
                    if (idx === -1) {
                        return { left: line.trim(), right: "" };
                    }
                    return {
                        left: line.slice(0, idx).trim(),
                        right: line.slice(idx + 1).trim(),
                    };
                })
                .filter((r) => r.left.length > 0 || r.right.length > 0);

            if (parsedRows.length === 0) {
                return originalElement;
            }

            const firstRight = parsedRows[0]?.right ?? "";

            return (
                <>
                    {originalElement}
                    <FoldoutPanel
                        header={<InfoRow left="BUILDING HEIGHT AND FOOTPRINT" disableFocus={true} right={firstRight} />}
                        initiallyExpanded={false}
                    >
                        {parsedRows.map((row, index) => (
                            <InfoRow
                                key={index}
                                left={row.left}
                                right={row.right}
                                disableFocus={true}
                            />
                        ))}
                    </FoldoutPanel>
                </>
            );
        };

    let wrappedAny = false;

    for (const key of candidateKeys) {
        const section = (componentList as any)[key];
        if (typeof section === "function") {
            (componentList as any)[key] = makeWrapper(section, key);
            wrappedAny = true;
        }
    }

    return componentList;
};
