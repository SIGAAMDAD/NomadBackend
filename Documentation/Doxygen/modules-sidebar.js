// --- HARDENED MODULE SIDEBAR ---------------------------------------------
(function () {
    console.log("[ModuleSidebar] Script loaded.");

    // Wait for an element to appear in the DOM
    function waitFor(selector, callback) {
        const el = document.querySelector(selector);
        if (el) {
            callback(el);
            return;
        }
        requestAnimationFrame(() => waitFor(selector, callback));
    }

    // Wait for navtree.js to finish populating the sidebar
    function waitForNavTree(callback) {
        const check = () => {
            const items = document.querySelectorAll("#nav-tree-contents a");
            if (items.length > 0) {
                console.log("[ModuleSidebar] Navtree detected with", items.length, "links.");
                callback();
            } else {
                requestAnimationFrame(check);
            }
        };
        check();
    }

    // Main entry point
    waitFor("#nav-tree-contents", () => {
        console.log("[ModuleSidebar] Sidebar container found.");

        waitForNavTree(() => {
            console.log("[ModuleSidebar] Navtree fully loaded. Initializing module sidebar…");
            initializeModuleSidebar();
        });
    });

    // ---------------------------------------------------------------------
    // Full module-sidebar logic
    // ---------------------------------------------------------------------
    function initializeModuleSidebar() {
        const sidebar = document.querySelector("#nav-tree-contents");
        if (!sidebar) {
            console.warn("[ModuleSidebar] Sidebar vanished unexpectedly.");
            return;
        }

        const projectNumberEl = document.querySelector("#projectnumber");
        const DOC_VERSION = projectNumberEl ? projectNumberEl.innerText.trim() : "unknown";

        const CACHE_KEY = "nomad_module_index_" + DOC_VERSION;
        const CACHE_TTL = 1000 * 60 * 60 * 24; // 24 hours

        console.log("[ModuleSidebar] Using cache key:", CACHE_KEY);

        // --- Utility: fetch a page and extract metadata ---
        async function extractMetadata(url) {
            try {
                const res = await fetch(url);
                const html = await res.text();
                const doc = new DOMParser().parseFromString(html, "text/html");

                const get = (selector, prefix = "") => {
                    const el = doc.querySelector(selector);
                    if (!el) return null;
                    return el.innerText.replace(prefix, "").trim();
                };

                return {
                    module: get(".meta-module", "Module:"),
                    category: get(".meta-category", "Category:"),
                    stability: get(".meta-stability", "Stability:")
                };
            } catch (e) {
                console.warn("[ModuleSidebar] Failed to extract metadata for", url, e);
                return { module: null, category: null, stability: null };
            }
        }

        // --- Step 1: collect all sidebar links ---
        const links = [...sidebar.querySelectorAll("a")].map(a => ({
            text: a.innerText.trim(),
            href: a.getAttribute("href")
        }));

        console.log("[ModuleSidebar] Found", links.length, "links in navtree.");

        // --- Step 2: build module index (with caching) ---
        async function buildModuleIndex() {
            try {
                const cachedRaw = localStorage.getItem(CACHE_KEY);
                if (cachedRaw) {
                    const cached = JSON.parse(cachedRaw);
                    if (cached && (Date.now() - cached.timestamp < CACHE_TTL)) {
                        console.log("[ModuleSidebar] Using cached module index.");
                        return cached.data;
                    }
                }
            } catch (e) {
                console.warn("[ModuleSidebar] Failed to read cache:", e);
            }

            console.log("[ModuleSidebar] Building module index from scratch…");

            const moduleMap = {};

            for (const link of links) {
                if (!link.href || link.href.startsWith("javascript:")) continue;

                const meta = await extractMetadata(link.href);
                if (!meta.module) continue;

                if (!moduleMap[meta.module]) moduleMap[meta.module] = [];

                moduleMap[meta.module].push({
                    ...link,
                    category: meta.category,
                    stability: meta.stability
                });
            }

            try {
                localStorage.setItem(
                    CACHE_KEY,
                    JSON.stringify({ timestamp: Date.now(), data: moduleMap })
                );
                console.log("[ModuleSidebar] Module index cached.");
            } catch (e) {
                console.warn("[ModuleSidebar] Failed to write cache:", e);
            }

            return moduleMap;
        }

        // --- Sorting rules ---
        function sortEntries(entries) {
            return entries.sort((a, b) => {
                // 1. Category
                if (a.category && b.category && a.category !== b.category) {
                    return a.category.localeCompare(b.category);
                }

                // 2. Stability
                if (a.stability && b.stability && a.stability !== b.stability) {
                    return a.stability.localeCompare(b.stability);
                }

                // 3. Name
                return a.text.localeCompare(b.text);
            });
        }

        // --- Step 3: render sidebar ---
        function renderModules(moduleMap) {
            console.log("[ModuleSidebar] Rendering module sidebar…");

            sidebar.innerHTML = "";

            // Search bar
            const search = document.createElement("input");
            search.className = "module-search";
            search.placeholder = "Search modules...";
            sidebar.appendChild(search);

            const title = document.createElement("div");
            title.className = "module-tree-title";
            title.innerText = "Modules";
            sidebar.appendChild(title);

            const ul = document.createElement("ul");
            ul.className = "module-tree-root";

            Object.keys(moduleMap)
                .sort()
                .forEach(moduleName => {
                    const li = document.createElement("li");
                    li.className = "module-node";

                    const header = document.createElement("div");
                    header.className = "module-header";
                    header.innerHTML = `▶ ${moduleName}`;
                    li.appendChild(header);

                    const sub = document.createElement("ul");
                    sub.className = "module-subtree";

                    const sorted = sortEntries(moduleMap[moduleName]);

                    sorted.forEach(entry => {
                        const li2 = document.createElement("li");
                        li2.className = "module-item";

                        const a = document.createElement("a");
                        a.href = entry.href;
                        a.innerText = entry.text;

                        if (entry.category || entry.stability) {
                            const meta = document.createElement("span");
                            meta.className = "module-meta";
                            meta.innerText = [
                                entry.category || "",
                                entry.stability || ""
                            ].filter(x => x).join(" • ");
                            li2.appendChild(meta);
                        }

                        li2.appendChild(a);
                        sub.appendChild(li2);
                    });

                    li.appendChild(sub);

                    header.addEventListener("click", () => {
                        li.classList.toggle("open");
                    });

                    ul.appendChild(li);
                });

            sidebar.appendChild(ul);

            // --- Search filter ---
            search.addEventListener("input", () => {
                const q = search.value.toLowerCase();

                document.querySelectorAll(".module-node").forEach(node => {
                    const header = node.querySelector(".module-header");
                    const moduleName = header ? header.innerText.toLowerCase() : "";
                    const items = [...node.querySelectorAll(".module-item")];

                    let visible = false;

                    items.forEach(item => {
                        const text = item.innerText.toLowerCase();
                        const match = text.includes(q);
                        item.style.display = match ? "" : "none";
                        if (match) visible = true;
                    });

                    node.style.display = visible || moduleName.includes(q) ? "" : "none";

                    // Auto-open modules that have matches
                    if (q && (visible || moduleName.includes(q))) {
                        node.classList.add("open");
                    }
                });
            });

            console.log("[ModuleSidebar] Module sidebar initialized.");
        }

        // --- Execute ---
        buildModuleIndex().then(renderModules).catch(err => {
            console.error("[ModuleSidebar] Failed to build module index:", err);
        });
    }
})();