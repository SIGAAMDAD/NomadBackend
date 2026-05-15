// ue5.js — UE5-like behavior for Doxygen pages
// - Build a right-hand "On This Page" TOC and keep it sticky
// - Toggle visibility of "Internal" docs (default hidden), persists in localStorage
// - Detect and tag "Private"/"Internal" content so we can hide it with CSS
// - Disable splitbar resizing to keep doc width fixed
// - Remove top-gap artifacts

(function () {
  const STORAGE_KEY = 'ue.showInternal';

  // Read persisted toggle state (default false = hidden)
  const showInternal = localStorage.getItem(STORAGE_KEY) === '1';

  // Apply body state early to avoid flicker
  if (!showInternal) {
    document.documentElement.classList.add('internal-hidden');
    document.body.classList.add('internal-hidden');
  }

  function $(sel, root) { return (root || document).querySelector(sel); }
  function $all(sel, root) { return Array.from((root || document).querySelectorAll(sel)); }

  // Utility: mark an element (or its nearest block) as internal
  function markInternal(el) {
    if (!el) return;
    const block = el.closest('.memitem, .memberdecls, .directory, .class, .namespace, .file, .groupheader, .contents, .textblock, table, li, div');
    (block || el).setAttribute('data-internal', 'true');
  }

  // Heuristics to detect internal/private content
  function detectInternal() {
    const textMatchesInternal = (txt) => /\b(private|internal)\b/i.test(txt || '');

    // 1) Page-level titles
    const headerTitle = $('#titlearea .title') || $('.headertitle') || $('.title') || $('h1');
    if (headerTitle && textMatchesInternal(headerTitle.textContent)) {
      const content = $('#doc-content') || document.body;
      markInternal(content);
    }

    // 2) Breadcrumb hints
    $all('.navpath a, .navpath li').forEach((a) => {
      if (textMatchesInternal(a.textContent)) {
        markInternal($('#doc-content'));
      }
    });

    // 3) Content candidates
    $all('.memitem, .memberdecls, .memdoc, .memname, .memproto, .directory, .class, .namespace, .file, .groupheader, table, tr, li, div, span, code, pre').forEach((el) => {
      const t = (el.textContent || '').trim();
      if (
        t.includes('/Private/') || t.includes('\\Private\\') ||
        t.includes('::Private') || t.includes('.Private') ||
        /\binternal\b/i.test(t)
      ) {
        markInternal(el);
      }
    });

    // 4) Left nav links (tree)
    $all('#nav-tree a, #nav-tree-contents a').forEach((a) => {
      if (textMatchesInternal(a.textContent)) a.classList.add('internal-link');
    });

    // 5) Search results
    $all('#MSearchResultsWindow .SRResult a').forEach((a) => {
      if (textMatchesInternal(a.textContent)) {
        const row = a.closest('.SRResult') || a.parentElement;
        if (row) row.setAttribute('data-internal', 'true');
      }
    });
  }

  // Build a right-hand TOC ("On This Page") from h2/h3 in doc-content
  function buildRightTOC() {
    const content = $('#doc-content') || document.body;
    const headings = $all('h2, h3', content).filter(h => h.id || (h.textContent || '').trim().length);
    if (!headings.length) return; // nothing to build

    let rightbar = $('#ue-rightbar');
    if (!rightbar) {
      rightbar = document.createElement('aside');
      rightbar.id = 'ue-rightbar';
      // Insert after doc-content in the DOM flow so CSS float positions it to the right
      content.parentNode.insertBefore(rightbar, content.nextSibling);
    }

    const title = document.createElement('div');
    title.className = 'ue-toc-title';
    title.textContent = 'On This Page';

    const nav = document.createElement('nav');
    const list = document.createElement('div');

    headings.forEach(h => {
      if (!h.id) {
        // create stable id
        const base = (h.textContent || 'section').trim().toLowerCase().replace(/[^\w]+/g, '-');
        let id = base || 'section';
        let i = 1;
        while (document.getElementById(id)) id = `${base}-${i++}`;
        h.id = id;
      }
      const a = document.createElement('a');
      a.href = `#${h.id}`;
      a.textContent = h.textContent || h.id;
      a.dataset.level = h.tagName.toLowerCase(); // h2/h3
      list.appendChild(a);
    });

    nav.appendChild(list);
    rightbar.innerHTML = '';
    rightbar.appendChild(title);
    rightbar.appendChild(nav);

    // Simple scrollspy
    const anchors = $all('#ue-rightbar a');
    function onScroll() {
      let current = null;
      const fromTop = window.scrollY + (parseInt(getComputedStyle(document.documentElement).getPropertyValue('--ue-header-height')) || 56) + 16;
      headings.forEach(h => {
        if (h.offsetTop <= fromTop) current = h;
      });
      anchors.forEach(a => a.classList.toggle('active', current && a.getAttribute('href') === `#${current.id}`));
    }
    document.addEventListener('scroll', onScroll, { passive: true });
    onScroll();
  }

  // Insert brand + internal toggle into #top (header)
  function enhanceHeader() {
    const header = document.getElementById('top') || document.body;

    // Brand (left)
    if (!document.getElementById('ue-brand')) {
      const brand = document.createElement('div');
      brand.id = 'ue-brand';

      const logoSrc = (function () {
        const img = document.querySelector('#projectlogo img, #titlearea img');
        return img ? img.getAttribute('src') : 'NomadFramework.png';
      })();
      const img = document.createElement('img');
      img.alt = 'NomadFramework';
      img.src = logoSrc;

      const name = document.createElement('span');
      name.className = 'name';
      name.textContent = 'NomadFramework API';

      brand.appendChild(img);
      brand.appendChild(name);
      header.prepend(brand);
    }

    // Ensure search box is right-aligned (Doxygen will place #MSearchBox)
    // Add internal toggle next to it
    let btn = document.getElementById('ue-internal-toggle');
    if (!btn) {
      btn = document.createElement('button');
      btn.id = 'ue-internal-toggle';
      btn.type = 'button';
      btn.textContent = showInternal ? 'Hide Internal Documentation' : 'Show Internal Documentation';
      btn.setAttribute('aria-pressed', showInternal ? 'true' : 'false');
      // Append after search
      const msearch = document.getElementById('MSearchBox');
      if (msearch && msearch.parentNode) {
        msearch.parentNode.insertBefore(btn, msearch.nextSibling);
      } else {
        header.appendChild(btn);
      }

      btn.addEventListener('click', () => {
        const hidden = document.documentElement.classList.contains('internal-hidden');
        if (hidden) {
          document.documentElement.classList.remove('internal-hidden');
          document.body.classList.remove('internal-hidden');
          localStorage.setItem(STORAGE_KEY, '1');
          btn.textContent = 'Hide Internal Documentation';
          btn.setAttribute('aria-pressed', 'true');
        } else {
          document.documentElement.classList.add('internal-hidden');
          document.body.classList.add('internal-hidden');
          localStorage.setItem(STORAGE_KEY, '0');
          btn.textContent = 'Show Internal Documentation';
          btn.setAttribute('aria-pressed', 'false');
        }
      });
    }
  }

  // Disable resizer / split bar to keep doc width fixed
  function disableResizable() {
    const splitbar = document.getElementById('splitbar');
    if (splitbar && splitbar.parentNode) splitbar.parentNode.removeChild(splitbar);
    if (typeof window.initResizable === 'function') {
      try { window.initResizable = function () {}; } catch (_) {}
    }
    $all('.ui-resizable-handle').forEach(h => h.parentNode && h.parentNode.removeChild(h));
  }

  // Tidy top-gap artifacts caused by stacked themes
  function collapseTopGap() {
    // Remove margin/padding on first child of #doc-content (reinforced by CSS)
    const first = ($('#doc-content') || document.body).firstElementChild;
    if (first) {
      first.style.marginTop = '0';
      first.style.paddingTop = (first.style.paddingTop || '0');
    }
    // Ensure any empty header blocks are hidden
    $all('.header').forEach(h => { if (!h.textContent.trim()) h.style.display = 'none'; });
  }

  document.addEventListener('DOMContentLoaded', () => {
    try {
      enhanceHeader();
      detectInternal();
      buildRightTOC();
      disableResizable();
      collapseTopGap();
    } catch (e) {
      console && console.warn && console.warn('UE5 skin init error:', e);
    }
  });

  // -------------------------------------------------------------
// UE‑STYLE METADATA CARD CREATION
// -------------------------------------------------------------

function buildMetadataCard() {
    const content = document.getElementById("doc-content");
    if (!content) return;

    // Extract raw HTML of main description area
    const blocks = content.querySelectorAll(".textblock, .memdoc");
    if (!blocks.length) return;

    let text = blocks[0].innerHTML;

    // Find metadata lines like @module X
    const metaRegex = /@(\w+)\s+([^\n<]+)/g;

    let metadata = {};
    let match;

    while ((match = metaRegex.exec(text)) !== null) {
        const key = match[1].trim().toLowerCase();
        const value = match[2].trim();
        metadata[key] = value;
    }

    if (Object.keys(metadata).length === 0) return;

    // Remove metadata tags from the page’s visible textblock(s)
    blocks.forEach(b => {
        b.innerHTML = b.innerHTML.replace(metaRegex, "");
    });

    // Build metadata card
    const card = document.createElement("div");
    card.className = "ue-meta-card";

    const title = document.createElement("h3");
    title.textContent = "Class Metadata";
    card.appendChild(title);

    const grid = document.createElement("div");
    grid.className = "ue-meta-grid";

    for (const [key, value] of Object.entries(metadata)) {
        const label = document.createElement("div");
        label.className = "ue-meta-label";
        label.textContent = key.charAt(0).toUpperCase() + key.slice(1);

        const val = document.createElement("div");
        val.className = "ue-meta-value";
        val.textContent = value;

        grid.appendChild(label);
        grid.appendChild(val);
    }

    card.appendChild(grid);

    // Insert card before first h1/h2 or before the first member section
    const insertBefore =
        content.querySelector("h1 + *") ||
        content.firstElementChild;

    content.insertBefore(card, insertBefore);
}

document.addEventListener("DOMContentLoaded", () => {
    try { buildMetadataCard(); } catch (e) {}
});
})();