import { computed, inject, type ComputedRef, type InjectionKey, type Plugin } from "vue";
import {
  ConfigError,
  defineConfig,
  staticSource,
  url,
  type ConfigSchema,
  type ConfigSource,
  type InferConfig,
} from "@wow-two-beta/fe/foundation/config";
import { AppSwitcherKind, type AppSwitcherItem } from "@wow-two-beta/fe/presentation/nav";
import { BookOpen, Globe, LayoutDashboard, Megaphone, Palette, PenLine, Shirt } from "lucide-vue-next";
import type { Component } from "vue";

import { notifyFeedback } from "./Feedback";
import {
  OcharoAppKind,
  ocharoApps,
  resolveOcharoAppUrl,
  type OcharoApp,
  type OcharoAppAddresses,
  type OcharoAppKey,
} from "./OcharoApps";
import { runtimeConfigApi, type RuntimeConfigValues } from "./RuntimeConfigApi";
import { useUiLocale } from "./UseUiLocale";

/** The build reads every key under this prefix: `OCHARO_DESK_URL` is `VITE_OCHARO_DESK_URL` there. */
export const BuildTimePrefix = "VITE_";

/**
 * Every address Marketing and Editorial read, keyed as `/api/runtime-config` serves it; each default is the local-run
 * address. The app keys match Desk's. `PLATFORM_URL` is the Desk address the connection panel opens; only the build
 * sets it (`VITE_PLATFORM_URL`), as before.
 */
export const OcharoAppsConfigSchema = {
  OCHARO_DESK_URL: url({ default: "https://localhost:8264/" }),
  OCHARO_CATALOGUE_URL: url({ default: "https://localhost:8282/catalogues" }),
  OCHARO_STUDIO_URL: url({ default: "https://localhost:8262/studio" }),
  OCHARO_MARKETING_URL: url({ default: "https://localhost:8358/" }),
  OCHARO_EDITORIAL_URL: url({ default: "https://localhost:8284/studio/" }),
  OCHARO_BRAND_URL: url({ default: "https://localhost:8288/" }),
  OCHARO_ATLAS_URL: url({ default: "https://localhost:8284/atlas/" }),
  OCHARO_WEAR_URL: url({ default: "https://localhost:8284/wear/" }),
  OCHARO_BLOG_URL: url({ default: "https://localhost:8284/" }),
  PLATFORM_URL: url({ default: "https://localhost:8264/" }),
} satisfies ConfigSchema;

/** The resolved addresses, one per schema key. */
export type OcharoAppsConfig = InferConfig<typeof OcharoAppsConfigSchema>;

/** @internal The menu destination each app key addresses. */
const AppKeys: Readonly<Partial<Record<keyof OcharoAppsConfig, OcharoAppKey>>> = {
  OCHARO_DESK_URL: "desk",
  OCHARO_CATALOGUE_URL: "catalogue",
  OCHARO_STUDIO_URL: "studio",
  OCHARO_MARKETING_URL: "marketing",
  OCHARO_EDITORIAL_URL: "editorial",
  OCHARO_BRAND_URL: "brand",
  OCHARO_ATLAS_URL: "atlas",
  OCHARO_WEAR_URL: "wear",
  OCHARO_BLOG_URL: "blog",
};

/** @internal The connection panel's Desk address when the configured one is unusable. */
const DefaultPlatformUrl = "https://localhost:8264/";

/** What the operator is told when startup could not use every source or value. */
export const OcharoAppsProblem = {
  Unavailable: "Ocharo app links could not be loaded, so the app menu uses the built-in addresses.",
  Rejected: "Some Ocharo app links are misconfigured, so the app menu leaves them out.",
} as const;

export type OcharoAppsProblem = (typeof OcharoAppsProblem)[keyof typeof OcharoAppsProblem];

/** The addresses an app links to, resolved once at startup. */
export interface OcharoLinks {
  /** The app menu's destinations. */
  readonly addresses: OcharoAppAddresses;
  /** The Desk address the connection panel opens. */
  readonly platformUrl: string;
}

/** What startup resolved, and each problem owed to the operator. */
export interface OcharoAppsStartup extends OcharoLinks {
  /** The operator message and the technical detail behind it; empty when every source and value was usable. */
  readonly problems: readonly { readonly message: OcharoAppsProblem; readonly detail: string }[];
}

/** The runtime source: the served settings under the build prefix, so one lookup key reads every source. */
export function runtimeConfigSource(values: RuntimeConfigValues): ConfigSource {
  return staticSource(
    Object.fromEntries(Object.entries(values).map(([key, value]) => [`${BuildTimePrefix}${key}`, value])),
  );
}

/**
 * Resolves every address, earlier sources first; throws one `ConfigError` naming each malformed value. The app passes
 * its build-time values as `importMetaEnvSource(import.meta.env)`, which only its own modules can read.
 */
export function resolveOcharoAppsConfig(sources: readonly ConfigSource[]): OcharoAppsConfig {
  return defineConfig(OcharoAppsConfigSchema, { sources, prefix: BuildTimePrefix });
}

/** @internal Maps resolved keys onto menu destinations and the connection panel's Desk address. */
function toLinks(config: Readonly<Record<string, unknown>>): OcharoLinks {
  const addresses: Partial<Record<OcharoAppKey, string>> = {};
  for (const [key, app] of Object.entries(AppKeys)) {
    const value = config[key];
    if (typeof value === "string" && app) addresses[app] = value;
  }
  const platform = config.PLATFORM_URL;
  return { addresses, platformUrl: typeof platform === "string" ? platform : DefaultPlatformUrl };
}

/** @internal Resolves the links; a malformed value leaves its key out instead of yielding to a lower source. */
function resolveLinks(sources: readonly ConfigSource[]): { links: OcharoLinks; error: ConfigError | null } {
  try {
    return { links: toLinks(resolveOcharoAppsConfig(sources)), error: null };
  } catch (error) {
    if (!(error instanceof ConfigError)) throw error;
    const rejected = new Set(error.issues.map((issue) => issue.key));
    const usable = Object.fromEntries(Object.entries(OcharoAppsConfigSchema).filter(([key]) => !rejected.has(key)));
    return { links: toLinks(defineConfig(usable, { sources, prefix: BuildTimePrefix })), error };
  }
}

/**
 * Reads the served settings once, then resolves every address over the app's build-time values and the local
 * defaults. A failed read keeps the build-time values; startup never waits longer than the bounded request.
 */
export async function loadOcharoApps(
  buildTime: ConfigSource,
  signal: AbortSignal = new AbortController().signal,
): Promise<OcharoAppsStartup> {
  const runtime = await runtimeConfigApi.get(signal);
  const sources = runtime.ok ? [runtimeConfigSource(runtime.value), buildTime] : [buildTime];
  const { links, error } = resolveLinks(sources);
  return {
    ...links,
    problems: [
      ...(runtime.ok
        ? []
        : [
            {
              message: OcharoAppsProblem.Unavailable,
              detail: `GET /api/runtime-config failed: ${runtime.failure.code}.`,
            },
          ]),
      ...(error ? [{ message: OcharoAppsProblem.Rejected, detail: error.message }] : []),
    ],
  };
}

/** Reports each startup problem once through the app feedback; the app keeps working with the addresses it has. */
export function reportOcharoAppsStartup(startup: OcharoAppsStartup): void {
  for (const problem of startup.problems) {
    console.warn(`Ocharo apps: ${problem.detail}`);
    notifyFeedback(problem.message, `ocharo-apps:${problem.message}`, "danger");
  }
}

/** @internal Carries the startup resolution to every component. */
const OcharoLinksKey: InjectionKey<OcharoLinks> = Symbol("OcharoLinks");

/** Provides the links resolved at startup to the whole app. */
export function ocharoAppsPlugin(links: OcharoLinks): Plugin {
  return {
    install(app) {
      app.provide(OcharoLinksKey, { addresses: links.addresses, platformUrl: links.platformUrl });
    },
  };
}

/** @internal The startup resolution, or the local defaults where none is provided. */
function useOcharoLinks(): OcharoLinks {
  return inject(OcharoLinksKey, () => resolveLinks([]).links, true);
}

/** The app menu's destinations. */
export function useOcharoApps(): OcharoApp[] {
  return ocharoApps(useOcharoLinks().addresses);
}

/** The Desk address the connection panel opens; only a plain HTTPS link, else the local Desk. */
export function usePlatformUrl(): string {
  return resolveOcharoAppUrl(useOcharoLinks().platformUrl) ?? DefaultPlatformUrl;
}

/** The icon on each app tile in the app menu, as Desk draws it; public sites share one. */
export const OcharoAppIcons: Readonly<Record<OcharoAppKey, Component>> = {
  desk: LayoutDashboard,
  catalogue: BookOpen,
  studio: Shirt,
  marketing: Megaphone,
  editorial: PenLine,
  brand: Palette,
  atlas: Globe,
  wear: Globe,
  blog: Globe,
};

/** The app menu's tiles in the operator language, for the SDK `AppSwitcherMenu`. */
export function useOcharoAppSwitcherItems(): ComputedRef<AppSwitcherItem[]> {
  const apps = useOcharoApps();
  const { t } = useUiLocale();
  return computed(() =>
    apps.map((app) => ({
      key: app.key,
      label: t(app.label),
      description: t(app.summary),
      href: app.url,
      icon: OcharoAppIcons[app.key],
      kind: app.kind === OcharoAppKind.Workspace ? AppSwitcherKind.App : AppSwitcherKind.Site,
    })),
  );
}
