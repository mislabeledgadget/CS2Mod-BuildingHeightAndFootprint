// index.tsx
import type { ModRegistrar } from "cs2/modding";
import { BuildingHeightAndFootprintComponent } from "mods/building-height-and-footprint-component";

const register: ModRegistrar = (moduleRegistry) =>
{
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        BuildingHeightAndFootprintComponent
    );
};

export default register;
