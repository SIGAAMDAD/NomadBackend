document.addEventListener("DOMContentLoaded", () => {
    const root = document.querySelector("#doc-content");
    if (!root) return;

    // Metadata sources
    const META_MAP = [
        { key: "Since", selector: "dl.section.since" },
        { key: "Version", selector: "dl.section.version" },
        { key: "Deprecated", selector: "dl.section.deprecated" },
        { key: "Warning", selector: "dl.section.warning" },
        { key: "Access Level", selector: ".meta-access" },
        { key: "Stability", selector: ".meta-stability" },
        { key: "Category", selector: ".meta-category" },
        { key: "Module", selector: ".meta-module" }
    ];

    // Icons (simple, readable, no external dependencies)
    const ICONS = {
        "Since": "📅",
        "Version": "🔢",
        "Deprecated": "⚠️",
        "Warning": "🚧",
        "Access Level": "🔒",
        "Stability": "🧪",
        "Category": "📁",
        "Module": "🧩"
    };

    const metadata = [];

    META_MAP.forEach(meta => {
        const el = root.querySelector(meta.selector);
        if (!el) return;

        let value = null;

        if (el.tagName.toLowerCase() === "dl") {
            const dd = el.querySelector("dd");
            if (dd) value = dd.innerText.trim();
        } else {
            value = el.innerText.replace(/^.*?:\s*/, "").trim();
        }

        if (value) metadata.push({ key: meta.key, value });

        el.remove();
    });

    if (metadata.length === 0) return;

    // --- Build panel ---
    const panel = document.createElement("div");
    panel.className = "meta-panel";

    // Header
    const header = document.createElement("div");
    header.className = "meta-header";
    header.innerHTML = `
        <span class="meta-header-title">Metadata</span>
        <span class="meta-header-icon">▶</span>
    `;

    // Content
    const content = document.createElement("div");
    content.className = "meta-content";

    content.innerHTML = metadata
        .map(m => `
            <div class="meta-row">
                <span class="meta-key">
                    <span class="meta-icon">${ICONS[m.key] || "•"}</span>
                    ${m.key}
                </span>
                <span class="meta-value">${m.value}</span>
            </div>
        `)
        .join("");

    panel.appendChild(header);
    panel.appendChild(content);

    // Insert under title
    const titleArea = document.querySelector("#titlearea");
    if (titleArea) titleArea.insertAdjacentElement("afterend", panel);

    // --- Collapsible behavior ---
    header.addEventListener("click", () => {
        panel.classList.toggle("open");

        if (panel.classList.contains("open")) {
            content.style.maxHeight = content.scrollHeight + "px";
        } else {
            content.style.maxHeight = "0px";
        }
    });
});