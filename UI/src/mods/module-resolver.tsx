// module-resolver.ts

import { ReactElement, ReactNode } from "react";
import { Theme } from "cs2/bindings";
import { getModule } from "cs2/modding";
import {
    ClassProps,
    FocusKey,
    InfoRowProps
    // you can add others later if you need them:
    // FormattedParagraphsProps,
    // TooltipProps,
} from "cs2/ui";

// Copy of the game's InfoSectionProps with children added.
// Their comment: the original lacks `children`, so we extend it.
export interface InfoSectionProps extends ClassProps {
    focusKey?: FocusKey;
    tooltip?: ReactElement | null;
    disableFocus?: boolean;
    children: any;
}

export interface FoldoutPanelProps extends ClassProps {
    header: ReactNode;
    initiallyExpanded?: boolean;
    expandFromContent?: boolean;
    focusKey?: FocusKey;
    tooltip?: ReactNode | null;
    disableFocus?: boolean;
    className?: string;
    onToggleExpanded?: (value: boolean) => void;
    children: any;
}

export class ModuleResolver {
    private static _instance: ModuleResolver = new ModuleResolver();
    public static get instance(): ModuleResolver { return this._instance; }

    // Cached modules
    private _infoRow: ((props: InfoRowProps) => ReactElement) | null = null;
    private _infoSection: ((props: InfoSectionProps) => ReactElement) | null = null;
    private _foldoutPanel: ((props: FoldoutPanelProps) => ReactElement) | null = null;

    private _infoRowClasses: Theme | any = null;

    // --- UI components ---

    public get InfoRow(): (props: InfoRowProps) => ReactElement {
        return this._infoRow ?? (
            this._infoRow = getModule(
                "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
                "InfoRow"
            )
        );
    }

    public get InfoSection(): (props: InfoSectionProps) => ReactElement {
        return this._infoSection ?? (
            this._infoSection = getModule(
                "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
                "InfoSection"
            )
        );
    }

    public get FoldoutPanel(): (props: FoldoutPanelProps) => ReactElement {
        return this._foldoutPanel ?? (
            this._foldoutPanel = getModule(
                "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section-foldout.tsx",
                "InfoSectionFoldout"
            )
        );
    }

    // --- SCSS modules (optional) ---

    public get InfoRowClasses(): Theme | any {
        return this._infoRowClasses ?? (
            this._infoRowClasses = getModule(
                "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
                "classes"
            )
        );
    }
}
