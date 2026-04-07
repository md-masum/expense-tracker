/* Finance Tracker PWA — Main Application
   Single-page app with hash-based routing and Bootstrap 5 UI. */

const App = (() => {
  /* ══════════════════════════════════════════════════════════════════════════
     UTILITIES
  ══════════════════════════════════════════════════════════════════════════ */

  const $ = id => document.getElementById(id);
  const content = () => $('app-content');

  /** Escape HTML to prevent XSS when inserting user data into innerHTML */
  function esc(str) {
    return String(str ?? '')
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function formatMoney(amount) {
    return Number(amount || 0).toLocaleString('en-BD', {
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }) + ' ৳';
  }

  function formatDate(dateStr) {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('en-GB', {
      day:   '2-digit',
      month: 'short',
      year:  'numeric',
    });
  }

  function todayISO() {
    return new Date().toISOString().split('T')[0];
  }

  function dateStamp() {
    const d = new Date();
    return d.getFullYear().toString() +
      String(d.getMonth() + 1).padStart(2, '0') +
      String(d.getDate()).padStart(2, '0');
  }

  function downloadFile(text, filename, mimeType) {
    const blob = new Blob([text], { type: mimeType });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }

  function loading() {
    content().innerHTML = `
      <div class="d-flex justify-content-center align-items-center" style="height:55vh">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading…</span>
        </div>
      </div>`;
  }

  /* ══════════════════════════════════════════════════════════════════════════
     MODAL
  ══════════════════════════════════════════════════════════════════════════ */

  let _bsModal = null;

  function getModal() {
    if (!_bsModal) _bsModal = new bootstrap.Modal($('appModal'));
    return _bsModal;
  }

  function showModal(title, bodyHTML, onSave) {
    $('modalTitle').textContent = title;
    $('modalBody').innerHTML    = bodyHTML;

    // Replace save button node to clear any previous listeners
    const oldBtn = $('modalSaveBtn');
    const newBtn = oldBtn.cloneNode(true);
    oldBtn.parentNode.replaceChild(newBtn, oldBtn);
    newBtn.addEventListener('click', async () => {
      if (newBtn.disabled) return;
      newBtn.disabled = true;
      try {
        await onSave();
      } finally {
        // Re-enable if modal is still present/open (e.g., validation error).
        if (document.body.contains(newBtn)) {
          newBtn.disabled = false;
        }
      }
    });

    getModal().show();

    // Focus first input after animation
    $('appModal').addEventListener('shown.bs.modal', () => {
      const first = $('modalBody').querySelector('input, select, textarea');
      if (first) first.focus();
    }, { once: true });
  }

  function hideModal() {
    getModal().hide();
  }

  /** Show a read-only informational modal (no Save button). */
  function showInfoModal(title, bodyHTML) {
    $('modalTitle').textContent = title;
    $('modalBody').innerHTML    = bodyHTML;
    const saveBtn = $('modalSaveBtn');
    saveBtn.style.display = 'none';
    $('appModal').addEventListener('hidden.bs.modal', () => {
      saveBtn.style.display = '';
    }, { once: true });
    getModal().show();
  }

  /* ══════════════════════════════════════════════════════════════════════════
     TOAST
  ══════════════════════════════════════════════════════════════════════════ */

  function showToast(message, type = 'success') {
    const el   = $('appToast');
    const body = $('toastBody');
    el.className = `toast align-items-center text-white border-0 bg-${type === 'success' ? 'success' : 'danger'}`;
    body.textContent = message;
    bootstrap.Toast.getOrCreateInstance(el, { delay: 3000 }).show();
  }

  /* ══════════════════════════════════════════════════════════════════════════
     ROUTER
  ══════════════════════════════════════════════════════════════════════════ */

  function setActiveNav(page) {
    document.querySelectorAll('[data-navlink]').forEach(el => {
      el.classList.toggle('active', el.dataset.navlink === page);
    });
    // Collapse mobile navbar
    const nav = $('navbarNav');
    if (nav && nav.classList.contains('show')) {
      const instance = bootstrap.Collapse.getInstance(nav);
      if (instance) instance.hide();
    }
  }

  async function route() {
    // Do not render if not authenticated
    if (!fbAuth.currentUser) return;

    const hash  = location.hash.replace(/^#\/?/, '') || 'dashboard';
    const parts = hash.split('/').filter(Boolean);
    const page  = parts[0] || 'dashboard';
    const param = parts[1];

    try {
      switch (page) {
        case 'dashboard':
          setActiveNav('dashboard');
          await renderDashboard();
          break;
        case 'projects':
          setActiveNav('projects');
          await renderProjects();
          break;
        case 'transactions':
          setActiveNav('projects');
          await renderTransactions(param);
          break;
        case 'categories':
          setActiveNav('categories');
          await renderCategories();
          break;
        case 'backup':
          setActiveNav('backup');
          await renderBackup();
          break;
        default:
          setActiveNav('dashboard');
          await renderDashboard();
      }
    } catch (err) {
      content().innerHTML = `
        <div class="alert alert-danger m-3">
          <strong>Error:</strong> ${esc(err.message)}
        </div>`;
      console.error(err);
    }
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PAGE: DASHBOARD
  ══════════════════════════════════════════════════════════════════════════ */

  async function renderDashboard() {
    loading();

    const projects = await FinanceDB.getAll('projects');

    if (projects.length === 0) {
      content().innerHTML = `
        <div class="page-container">
          <h4 class="page-title"><i class="bi bi-speedometer2 me-2 text-primary"></i>Dashboard</h4>
          <div class="alert alert-info">
            No projects yet. <a href="#/projects" class="alert-link fw-semibold">Create your first project</a>.
          </div>
        </div>`;
      return;
    }

    const summaries = await Promise.all(projects.map(async p => {
      const txs     = await FinanceDB.getTransactionsByProject(p.id);
      const income  = txs.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0);
      const expense = txs.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0);
      return { project: p, income, expense, balance: income - expense, count: txs.length };
    }));

    const totalIncome  = summaries.reduce((s, x) => s + x.income,  0);
    const totalExpense = summaries.reduce((s, x) => s + x.expense, 0);
    const totalBalance = totalIncome - totalExpense;

    const overviewCards = `
      <div class="row g-3 mb-4 dashboard-summary-row">
        <div class="col-12 col-sm-4">
          <div class="card summary-card income text-center h-100 shadow-sm">
            <div class="card-body py-3">
              <div class="stat-value text-success">${formatMoney(totalIncome)}</div>
              <div class="stat-label text-muted">Total Income</div>
            </div>
          </div>
        </div>
        <div class="col-12 col-sm-4">
          <div class="card summary-card expense text-center h-100 shadow-sm">
            <div class="card-body py-3">
              <div class="stat-value text-danger">${formatMoney(totalExpense)}</div>
              <div class="stat-label text-muted">Total Expense</div>
            </div>
          </div>
        </div>
        <div class="col-12 col-sm-4">
          <div class="card summary-card ${totalBalance >= 0 ? 'balance-positive' : 'balance-negative'} text-center h-100 shadow-sm">
            <div class="card-body py-3">
              <div class="stat-value ${totalBalance >= 0 ? 'text-primary' : 'text-warning'}">${formatMoney(totalBalance)}</div>
              <div class="stat-label text-muted">Net Balance</div>
            </div>
          </div>
        </div>
      </div>`;

    const projectCards = summaries.map(s => `
      <div class="col-12 col-md-6 col-xl-4">
        <div class="card project-card shadow-sm h-100">
          <div class="card-header d-flex justify-content-between align-items-center">
            <span class="fw-semibold text-truncate me-2">${esc(s.project.name)}</span>
            <span class="badge bg-secondary flex-shrink-0">${esc(s.project.type)}</span>
          </div>
          <div class="card-body">
            <div class="row text-center g-2">
              <div class="col-4">
                <div class="fw-bold text-success mini-stat">${formatMoney(s.income)}</div>
                <div class="text-muted mini-label">Income</div>
              </div>
              <div class="col-4">
                <div class="fw-bold text-danger mini-stat">${formatMoney(s.expense)}</div>
                <div class="text-muted mini-label">Expense</div>
              </div>
              <div class="col-4">
                <div class="fw-bold ${s.balance >= 0 ? 'text-primary' : 'text-warning'} mini-stat">${formatMoney(s.balance)}</div>
                <div class="text-muted mini-label">Balance</div>
              </div>
            </div>
          </div>
          <div class="card-footer d-flex justify-content-between align-items-center">
            <small class="text-muted">${s.count} transaction${s.count !== 1 ? 's' : ''}</small>
            <a href="#/transactions/${s.project.id}" class="btn btn-sm btn-outline-primary">
              <i class="bi bi-list-ul me-1"></i>View
            </a>
          </div>
        </div>
      </div>`).join('');

    content().innerHTML = `
      <div class="page-container">
        <h4 class="page-title"><i class="bi bi-speedometer2 me-2 text-primary"></i>Dashboard</h4>
        ${overviewCards}
        <h6 class="text-muted mb-3">Projects (${summaries.length})</h6>
        <div class="row g-3">${projectCards}</div>
      </div>`;
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PAGE: PROJECTS
  ══════════════════════════════════════════════════════════════════════════ */

  async function renderProjects() {
    loading();
    const projects = await FinanceDB.getAll('projects');

    const listItems = projects.length === 0
      ? `<div class="alert alert-info">No projects yet. Tap "New Project" to get started.</div>`
      : projects.map(p => `
          <div class="list-group-item list-group-item-action d-flex justify-content-between align-items-start py-3">
            <div class="me-2 overflow-hidden">
              <div class="fw-semibold text-truncate">
                ${esc(p.name)}
                <span class="badge bg-secondary ms-2">${esc(p.type)}</span>
              </div>
              <small class="text-muted">Created ${formatDate(p.createdAt)}</small>
            </div>
            <div class="d-flex gap-2 flex-shrink-0">
              <a href="#/transactions/${p.id}" class="btn btn-sm btn-outline-primary" title="Transactions">
                <i class="bi bi-list-ul"></i>
              </a>
              <button class="btn btn-sm btn-outline-secondary" onclick="App.editProject('${p.id}')" title="Edit">
                <i class="bi bi-pencil"></i>
              </button>
              <button class="btn btn-sm btn-outline-danger" onclick="App.deleteProject('${p.id}')" title="Delete">
                <i class="bi bi-trash"></i>
              </button>
            </div>
          </div>`).join('');

    content().innerHTML = `
      <div class="page-container">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h4 class="page-title mb-0"><i class="bi bi-folder me-2 text-primary"></i>Projects</h4>
          <button class="btn btn-primary btn-sm" onclick="App.addProject()">
            <i class="bi bi-plus-lg me-1"></i>New Project
          </button>
        </div>
        <div class="list-group shadow-sm">${listItems}</div>
      </div>`;
  }

  function projectFormHTML(p = {}) {
    const types = ['Construction', 'Agriculture', 'Business', 'Other'];
    return `
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fProjectName">
          Project Name <span class="text-danger">*</span>
        </label>
        <input type="text" class="form-control" id="fProjectName"
               value="${esc(p.name || '')}" placeholder="e.g. House Construction" maxlength="100" />
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fProjectType">
          Type <span class="text-danger">*</span>
        </label>
        <select class="form-select" id="fProjectType">
          <option value="">— Select Type —</option>
          ${types.map(t =>
            `<option value="${t}" ${p.type === t ? 'selected' : ''}>${t}</option>`
          ).join('')}
        </select>
      </div>
      <div id="fProjectError" class="text-danger small mt-1"></div>`;
  }

  async function addProject() {
    showModal('New Project', projectFormHTML(), async () => {
      const name = $('fProjectName').value.trim();
      const type = $('fProjectType').value;
      if (!name || !type) {
        $('fProjectError').textContent = 'Name and type are required.';
        return;
      }
      await FinanceDB.addItem('projects', {
        name, type,
        createdAt: new Date().toISOString(),
        isActive:  true,
      });
      hideModal();
      showToast('Project created!');
      await renderProjects();
    });
  }

  async function editProject(id) {
    const p = await FinanceDB.getById('projects', id);
    if (!p) return;
    showModal('Edit Project', projectFormHTML(p), async () => {
      const name = $('fProjectName').value.trim();
      const type = $('fProjectType').value;
      if (!name || !type) {
        $('fProjectError').textContent = 'Name and type are required.';
        return;
      }
      await FinanceDB.putItem('projects', { ...p, name, type });
      hideModal();
      showToast('Project updated!');
      await renderProjects();
    });
  }

  async function deleteProject(id) {
    if (!confirm('Delete this project and all its transactions?\n\nThis action cannot be undone.')) return;

    const removedCount = await FinanceDB.deleteTransactionsByProject(id);
    await FinanceDB.deleteItem('projects', id);

    const msg = removedCount > 0
      ? `Project deleted (${removedCount} transaction${removedCount !== 1 ? 's' : ''} removed).`
      : 'Project deleted.';
    showToast(msg, 'danger');
    await renderProjects();
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PAGE: TRANSACTIONS
  ══════════════════════════════════════════════════════════════════════════ */

  // In-memory state for the transactions page (filter + pagination)
  let _txState = {
    projectId: null,
    all:        [],   // full sorted list from DB
    catMap:     {},
    categories: [],
    page:       1,
    pageSize:   25,
  };

  async function renderTransactions(projectId) {
    loading();

    const [project, transactions, categories] = await Promise.all([
      FinanceDB.getById('projects', projectId),
      FinanceDB.getTransactionsByProject(projectId),
      FinanceDB.getAll('categories'),
    ]);

    if (!project) {
      content().innerHTML = `
        <div class="page-container">
          <div class="alert alert-warning">
            Project not found. <a href="#/projects" class="alert-link">Go to Projects</a>
          </div>
        </div>`;
      return;
    }

    const catMap  = Object.fromEntries(categories.map(c => [c.id, c.name]));
    const income  = transactions.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0);
    const expense = transactions.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0);
    const balance = income - expense;

    // Store state for filter/pagination callbacks
    _txState = { projectId, all: transactions, catMap, categories, page: 1, pageSize: 25 };

    const catFilterOpts = `<option value="">All Categories</option>` +
      categories.map(c => `<option value="${c.id}">${esc(c.name)}</option>`).join('');

    content().innerHTML = `
      <div class="page-container">
        <div class="mb-3">
          <a href="#/projects" class="btn btn-sm btn-outline-secondary">
            <i class="bi bi-arrow-left me-1"></i>Back
          </a>
        </div>

        <div class="d-flex justify-content-between align-items-start mb-3 flex-wrap gap-2">
          <div>
            <h4 class="mb-0 fw-bold text-truncate">${esc(project.name)}</h4>
            <span class="badge bg-secondary">${esc(project.type)}</span>
          </div>
          <button class="btn btn-primary btn-sm flex-shrink-0" onclick="App.addTransaction('${projectId}')">
            <i class="bi bi-plus-lg me-1"></i>Add Transaction
          </button>
        </div>

        <div class="row g-2 mb-3 dashboard-summary-row">
          <div class="col-12 col-sm-4">
            <div class="card summary-card income text-center h-100 shadow-sm">
              <div class="card-body py-2 px-1">
                <div class="fw-bold text-success mini-stat">${formatMoney(income)}</div>
                <div class="text-muted mini-label">Income</div>
              </div>
            </div>
          </div>
          <div class="col-12 col-sm-4">
            <div class="card summary-card expense text-center h-100 shadow-sm">
              <div class="card-body py-2 px-1">
                <div class="fw-bold text-danger mini-stat">${formatMoney(expense)}</div>
                <div class="text-muted mini-label">Expense</div>
              </div>
            </div>
          </div>
          <div class="col-12 col-sm-4">
            <div class="card summary-card ${balance >= 0 ? 'balance-positive' : 'balance-negative'} text-center h-100 shadow-sm">
              <div class="card-body py-2 px-1">
                <div class="fw-bold ${balance >= 0 ? 'text-primary' : 'text-warning'} mini-stat">${formatMoney(balance)}</div>
                <div class="text-muted mini-label">Balance</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Search & Filter bar -->
        <div class="card shadow-sm mb-3">
          <div class="card-body py-2 px-3">
            <div class="row g-2">
              <div class="col-12 col-sm-5">
                <input type="search" class="form-control form-control-sm" id="txSearch"
                       placeholder="Search by note…" oninput="App.applyTxFilters()" />
              </div>
              <div class="col-6 col-sm-4">
                <select class="form-select form-select-sm" id="txFilterType" onchange="App.applyTxFilters()">
                  <option value="">All Types</option>
                  <option value="Income">Income</option>
                  <option value="Expense">Expense</option>
                </select>
              </div>
              <div class="col-6 col-sm-3">
                <select class="form-select form-select-sm" id="txFilterCat" onchange="App.applyTxFilters()">
                  ${catFilterOpts}
                </select>
              </div>
            </div>
          </div>
        </div>

        <!-- Transaction list + pagination -->
        <div id="txListContainer"></div>
      </div>`;

    _renderTxList();
  }

  /** Called by search/filter controls — resets to page 1 and re-renders the list. */
  function applyTxFilters() {
    _txState.page = 1;
    _renderTxList();
  }

  /** Called by pagination buttons. */
  function txGoToPage(page) {
    _txState.page = page;
    _renderTxList();
    const el = $('txListContainer');
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  /** Filters, paginates and renders the transaction list into #txListContainer. */
  function _renderTxList() {
    const { all, catMap, pageSize, projectId } = _txState;
    const search = ($('txSearch')?.value || '').toLowerCase().trim();
    const typeF  = $('txFilterType')?.value || '';
    const catF   = $('txFilterCat')?.value  || '';

    const filtered = all.filter(tx => {
      if (typeF && tx.type !== typeF) return false;
      if (catF  && tx.categoryId !== catF) return false;
      if (search && !(tx.note || '').toLowerCase().includes(search)) return false;
      return true;
    });

    const total      = filtered.length;
    const totalPages = Math.max(1, Math.ceil(total / pageSize));
    const page       = Math.min(_txState.page, totalPages);
    _txState.page    = page;
    const start      = (page - 1) * pageSize;
    const pageItems  = filtered.slice(start, start + pageSize);

    const noResultMsg = all.length === 0
      ? 'No transactions yet. Tap "Add" to record one.'
      : 'No transactions match your search / filter.';

    const listHTML = pageItems.length === 0
      ? `<div class="alert alert-info">${noResultMsg}</div>`
      : `<div class="list-group shadow-sm">` +
          pageItems.map(tx => `
            <div class="list-group-item transaction-item ${tx.type === 'Income' ? 'income' : 'expense'} py-3">
              <div class="d-flex justify-content-between align-items-start gap-2 tx-item-row">
                <div class="overflow-hidden">
                  <div>
                    <span class="badge ${tx.type === 'Income' ? 'bg-success' : 'bg-danger'} me-1">${tx.type}</span>
                    <a href="javascript:void(0)" class="fw-bold text-decoration-none"
                       onclick="App.showTransactionDetail('${tx.id}', '${projectId}')">${esc(catMap[tx.categoryId] || 'Unknown')}</a>
                  </div>
                  <small class="text-muted d-block mt-1">
                    ${formatDate(tx.date)}${tx.note ? ' — ' + esc(tx.note) : ''}
                  </small>
                </div>
                <div class="text-end flex-shrink-0 tx-item-actions">
                  <div class="fw-bold ${tx.type === 'Income' ? 'text-success' : 'text-danger'} mb-1">
                    ${tx.type === 'Income' ? '+' : '−'}${formatMoney(tx.amount)}
                  </div>
                  <div class="d-flex gap-1 justify-content-end">
                    <button class="btn btn-sm btn-outline-secondary"
                            onclick="App.editTransaction('${tx.id}', '${projectId}')" title="Edit">
                      <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn btn-sm btn-outline-danger"
                            onclick="App.deleteTransaction('${tx.id}', '${projectId}')" title="Delete">
                      <i class="bi bi-trash"></i>
                    </button>
                  </div>
                </div>
              </div>
            </div>`).join('') +
        `</div>`;

    const paginationHTML = totalPages <= 1 ? '' : `
      <div class="d-flex justify-content-between align-items-center mt-3 flex-wrap gap-2">
        <small class="text-muted">
          Showing ${start + 1}–${Math.min(start + pageSize, total)} of ${total}
        </small>
        <nav>
          <ul class="pagination pagination-sm mb-0">
            <li class="page-item ${page === 1 ? 'disabled' : ''}">
              <a class="page-link" href="javascript:void(0)" onclick="App.txGoToPage(${page - 1})">‹</a>
            </li>
            ${_paginationRange(page, totalPages).map(p =>
              p === '…'
                ? `<li class="page-item disabled"><span class="page-link">…</span></li>`
                : `<li class="page-item ${p === page ? 'active' : ''}">
                     <a class="page-link" href="javascript:void(0)" onclick="App.txGoToPage(${p})">${p}</a>
                   </li>`
            ).join('')}
            <li class="page-item ${page === totalPages ? 'disabled' : ''}">
              <a class="page-link" href="javascript:void(0)" onclick="App.txGoToPage(${page + 1})">›</a>
            </li>
          </ul>
        </nav>
      </div>`;

    $('txListContainer').innerHTML = listHTML + paginationHTML;
  }

  /** Returns page numbers with '…' gaps for the pagination bar. */
  function _paginationRange(current, total) {
    const set = new Set([1, total, current, current - 1, current + 1].filter(p => p >= 1 && p <= total));
    const sorted = [...set].sort((a, b) => a - b);
    const result = [];
    let prev = 0;
    for (const p of sorted) {
      if (p - prev > 1) result.push('…');
      result.push(p);
      prev = p;
    }
    return result;
  }

  // Temp storage for category list used by the inline onchange handler
  let _txCategoriesCache = [];

  function txFormHTML(categories, tx = {}) {
    _txCategoriesCache = categories;
    const type    = tx.type || 'Expense';
    const catOpts = _categoryOptions(categories, type, tx.categoryId);

    return `
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fTxType">Type</label>
        <select class="form-select" id="fTxType" onchange="App.refreshTxCategories(this.value)">
          <option value="Expense" ${type === 'Expense' ? 'selected' : ''}>Expense</option>
          <option value="Income"  ${type === 'Income'  ? 'selected' : ''}>Income</option>
        </select>
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fTxCategory">
          Category <span class="text-danger">*</span>
        </label>
        <select class="form-select" id="fTxCategory">${catOpts}</select>
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fTxAmount">
          Amount (৳) <span class="text-danger">*</span>
        </label>
        <input type="number" class="form-control" id="fTxAmount"
               value="${tx.amount || ''}" min="0.01" step="any" placeholder="0" />
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fTxDate">Date</label>
        <input type="date" class="form-control" id="fTxDate"
               value="${tx.date ? tx.date.split('T')[0] : todayISO()}" />
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fTxNote">Note <span class="text-muted fw-normal">(optional)</span></label>
        <textarea class="form-control" id="fTxNote" rows="4"
                  placeholder="Add a note…" maxlength="500">${esc(tx.note || '')}</textarea>
      </div>
      <div id="fTxError" class="text-danger small mt-1"></div>`;
  }

  function _categoryOptions(categories, type, selectedId) {
    const filtered = categories.filter(c => c.type === type);
    return `<option value="">— Select Category —</option>` +
      filtered.map(c =>
        `<option value="${c.id}" ${c.id === selectedId ? 'selected' : ''}>${esc(c.name)}</option>`
      ).join('');
  }

  function refreshTxCategories(type) {
    const sel = $('fTxCategory');
    if (sel) sel.innerHTML = _categoryOptions(_txCategoriesCache, type, null);
  }

  async function addTransaction(projectId) {
    const categories = await FinanceDB.getAll('categories');
    showModal('New Transaction', txFormHTML(categories), async () => {
      const type       = $('fTxType').value;
      const categoryId = $('fTxCategory').value;
      const amount     = parseFloat($('fTxAmount').value);
      const date       = $('fTxDate').value;
      const note       = $('fTxNote').value.trim();

      if (!categoryId)            { $('fTxError').textContent = 'Please select a category.'; return; }
      if (!amount || amount <= 0) { $('fTxError').textContent = 'Amount must be greater than zero.'; return; }
      if (!date)                  { $('fTxError').textContent = 'Please pick a date.'; return; }

      const seqNo = await FinanceDB.getNextProjectSeq(projectId);
      await FinanceDB.addItem('transactions', {
        projectId: String(projectId),
        categoryId, amount, type, note, date, seqNo,
        createdAt: new Date().toISOString(),
      });
      hideModal();
      showToast('Transaction added!');
      await renderTransactions(projectId);
    });
  }

  async function editTransaction(id, projectId) {
    const [tx, categories] = await Promise.all([
      FinanceDB.getById('transactions', id),
      FinanceDB.getAll('categories'),
    ]);
    if (!tx) return;
    showModal('Edit Transaction', txFormHTML(categories, tx), async () => {
      const type       = $('fTxType').value;
      const categoryId = $('fTxCategory').value;
      const amount     = parseFloat($('fTxAmount').value);
      const date       = $('fTxDate').value;
      const note       = $('fTxNote').value.trim();

      if (!categoryId)           { $('fTxError').textContent = 'Please select a category.'; return; }
      if (!amount || amount <= 0) { $('fTxError').textContent = 'Amount must be greater than zero.'; return; }

      await FinanceDB.putItem('transactions', { ...tx, categoryId, amount, type, note, date });
      hideModal();
      showToast('Transaction updated!');
      await renderTransactions(projectId);
    });
  }

  async function deleteTransaction(id, projectId) {
    if (!confirm('Delete this transaction?')) return;
    await FinanceDB.deleteItem('transactions', id);
    showToast('Deleted.', 'danger');
    await renderTransactions(projectId);
  }

  async function showTransactionDetail(txId, projectId) {
    const [tx, categories] = await Promise.all([
      FinanceDB.getById('transactions', txId),
      FinanceDB.getAll('categories'),
    ]);
    if (!tx) return;
    const catName = (categories.find(c => c.id === tx.categoryId) || {}).name || 'Unknown';
    const noteDisplay = tx.note
      ? `<div class="border rounded p-3 bg-light" style="min-height:120px;white-space:pre-wrap;word-break:break-word">${esc(tx.note)}</div>`
      : `<div class="border rounded p-3 bg-light text-muted" style="min-height:120px"><em>No note</em></div>`;

    showInfoModal('Transaction Details', `
      <div class="mb-3 d-flex gap-2 align-items-center">
        <span class="badge ${tx.type === 'Income' ? 'bg-success' : 'bg-danger'} fs-6">${esc(tx.type)}</span>
        <span class="fw-semibold fs-5">${esc(catName)}</span>
      </div>
      <div class="row g-2 mb-3">
        <div class="col-6">
          <div class="text-muted small">Amount</div>
          <div class="fw-bold fs-5 ${tx.type === 'Income' ? 'text-success' : 'text-danger'}">
            ${tx.type === 'Income' ? '+' : '−'}${formatMoney(tx.amount)}
          </div>
        </div>
        <div class="col-6">
          <div class="text-muted small">Date</div>
          <div class="fw-semibold">${formatDate(tx.date)}</div>
        </div>
      </div>
      <div>
        <div class="text-muted small mb-1">Note</div>
        ${noteDisplay}
      </div>`);
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PAGE: CATEGORIES
  ══════════════════════════════════════════════════════════════════════════ */

  async function renderCategories() {
    loading();
    const categories = await FinanceDB.getAll('categories');
    const expList = categories.filter(c => c.type === 'Expense');
    const incList = categories.filter(c => c.type === 'Income');

    function buildList(cats) {
      if (cats.length === 0) return `<p class="text-muted small mb-0">None yet.</p>`;
      return `
        <div class="list-group list-group-flush">
          ${cats.map(c => `
            <div class="list-group-item d-flex justify-content-between align-items-center py-2">
              <span>${esc(c.name)}</span>
              <div class="d-flex gap-2">
                <button class="btn btn-sm btn-outline-secondary" onclick="App.editCategory('${c.id}')" title="Edit">
                  <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-sm btn-outline-danger" onclick="App.deleteCategory('${c.id}')" title="Delete">
                  <i class="bi bi-trash"></i>
                </button>
              </div>
            </div>`).join('')}
        </div>`;
    }

    content().innerHTML = `
      <div class="page-container">
        <div class="d-flex justify-content-between align-items-center mb-4">
          <h4 class="page-title mb-0"><i class="bi bi-tags me-2 text-primary"></i>Categories</h4>
          <button class="btn btn-primary btn-sm" onclick="App.addCategory()">
            <i class="bi bi-plus-lg me-1"></i>New Category
          </button>
        </div>

        <div class="card shadow-sm mb-3">
          <div class="card-header d-flex align-items-center fw-semibold text-danger">
            <i class="bi bi-dash-circle me-2"></i>Expense Categories
            <span class="badge bg-danger ms-auto">${expList.length}</span>
          </div>
          <div class="card-body p-2">${buildList(expList)}</div>
        </div>

        <div class="card shadow-sm">
          <div class="card-header d-flex align-items-center fw-semibold text-success">
            <i class="bi bi-plus-circle me-2"></i>Income Categories
            <span class="badge bg-success ms-auto">${incList.length}</span>
          </div>
          <div class="card-body p-2">${buildList(incList)}</div>
        </div>
      </div>`;
  }

  function catFormHTML(c = {}) {
    return `
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fCatName">
          Name <span class="text-danger">*</span>
        </label>
        <input type="text" class="form-control" id="fCatName"
               value="${esc(c.name || '')}" placeholder="e.g. Labour" maxlength="60" />
      </div>
      <div class="mb-3">
        <label class="form-label fw-semibold" for="fCatType">Type</label>
        <select class="form-select" id="fCatType">
          <option value="Expense" ${c.type === 'Expense' ? 'selected' : ''}>Expense</option>
          <option value="Income"  ${c.type === 'Income'  ? 'selected' : ''}>Income</option>
        </select>
      </div>
      <div id="fCatError" class="text-danger small mt-1"></div>`;
  }

  async function addCategory() {
    showModal('New Category', catFormHTML(), async () => {
      const name = $('fCatName').value.trim();
      const type = $('fCatType').value;
      if (!name) { $('fCatError').textContent = 'Name is required.'; return; }
      await FinanceDB.addItem('categories', { name, type });
      hideModal();
      showToast('Category created!');
      await renderCategories();
    });
  }

  async function editCategory(id) {
    const c = await FinanceDB.getById('categories', id);
    if (!c) return;
    showModal('Edit Category', catFormHTML(c), async () => {
      const name = $('fCatName').value.trim();
      const type = $('fCatType').value;
      if (!name) { $('fCatError').textContent = 'Name is required.'; return; }
      await FinanceDB.putItem('categories', { ...c, name, type });
      hideModal();
      showToast('Category updated!');
      await renderCategories();
    });
  }

  async function deleteCategory(id) {
    if (!confirm('Delete this category?\n\nExisting transactions using it will show "Unknown".')) return;
    await FinanceDB.deleteItem('categories', id);
    showToast('Deleted.', 'danger');
    await renderCategories();
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PAGE: BACKUP
  ══════════════════════════════════════════════════════════════════════════ */

  async function renderBackup() {
    loading();
    const projects = await FinanceDB.getAll('projects');

    const projectOptions = projects.length === 0
      ? `<option value="">No projects yet</option>`
      : `<option value="">— Select Project —</option>` +
        projects.map(p => `<option value="${p.id}">${esc(p.name)}</option>`).join('');

    content().innerHTML = `
      <div class="page-container">
        <h4 class="page-title"><i class="bi bi-cloud-arrow-up me-2 text-primary"></i>Backup &amp; Export</h4>

        <!-- JSON Backup -->
        <div class="card shadow-sm mb-3">
          <div class="card-header fw-semibold">
            <i class="bi bi-cloud-arrow-down me-2 text-primary"></i>Full Backup (JSON)
          </div>
          <div class="card-body">
            <p class="text-muted small mb-3">
              Exports all projects, categories, and transactions into one JSON file.
              Save this to Google Drive, WhatsApp, or email it to yourself.
            </p>
            <button class="btn btn-primary" onclick="App.exportJSON()">
              <i class="bi bi-download me-2"></i>Export Backup
            </button>
          </div>
        </div>

        <!-- CSV Report -->
        <div class="card shadow-sm mb-3">
          <div class="card-header fw-semibold">
            <i class="bi bi-file-earmark-spreadsheet me-2 text-success"></i>Project Report (CSV)
          </div>
          <div class="card-body">
            <p class="text-muted small mb-3">
              Export transactions for one project as a spreadsheet-friendly CSV.
            </p>
            <div class="d-flex gap-2 flex-wrap align-items-center">
              <select class="form-select" id="csvProjectSelect" style="max-width:260px">
                ${projectOptions}
              </select>
              <button class="btn btn-success" onclick="App.exportCSV()">
                <i class="bi bi-filetype-csv me-1"></i>Export CSV
              </button>
            </div>
          </div>
        </div>

        <!-- Restore -->
        <div class="card shadow-sm mb-3">
          <div class="card-header fw-semibold">
            <i class="bi bi-cloud-upload me-2 text-warning"></i>Restore from Backup
          </div>
          <div class="card-body">
            <p class="small mb-3">
              <span class="badge bg-danger me-1">Warning</span>
              Restoring will <strong>replace all current data</strong>. Export a backup first.
            </p>
            <div class="mb-3">
              <input type="file" class="form-control" id="importFile" accept=".json" />
            </div>
            <button class="btn btn-warning" onclick="App.importJSON()">
              <i class="bi bi-upload me-2"></i>Restore Backup
            </button>
          </div>
        </div>

        <!-- Tips -->
        <div class="card border-secondary-subtle">
          <div class="card-body text-muted small">
            <strong><i class="bi bi-lightbulb me-1 text-warning"></i>Tips:</strong>
            <ul class="mb-0 mt-2 ps-3">
              <li>Data is synced to Firebase — accessible from any device after signing in</li>
              <li>Export JSON backups regularly as an extra safety net</li>
              <li>CSV files open in Excel, Google Sheets, or Apple Numbers</li>
            </ul>
          </div>
        </div>
      </div>`;
  }

  async function exportJSON() {
    try {
      const [projects, categories, transactions] = await Promise.all([
        FinanceDB.getAll('projects'),
        FinanceDB.getAll('categories'),
        FinanceDB.getAll('transactions'),
      ]);

      const backup = {
        appVersion:  '1.0',
        exportedAt:  new Date().toISOString(),
        projects, categories, transactions,
      };

      downloadFile(
        JSON.stringify(backup, null, 2),
        `FinanceTracker_Backup_${dateStamp()}.json`,
        'application/json'
      );
      showToast('Backup downloaded!');
    } catch (e) {
      showToast('Export failed: ' + e.message, 'danger');
    }
  }

  async function exportCSV() {
    const projectId = $('csvProjectSelect')?.value;
    if (!projectId) { showToast('Please select a project first.', 'danger'); return; }

    try {
      const [project, transactions, categories] = await Promise.all([
        FinanceDB.getById('projects', projectId),
        FinanceDB.getTransactionsByProject(projectId),
        FinanceDB.getAll('categories'),
      ]);

      const catMap = Object.fromEntries(categories.map(c => [c.id, c.name]));
      const sorted = [...transactions].sort((a, b) => new Date(a.date) - new Date(b.date));

      function csvCell(val) {
        const s = String(val ?? '');
        return s.includes(',') || s.includes('"') || s.includes('\n')
          ? `"${s.replace(/"/g, '""')}"`
          : s;
      }

      const totalIncome  = sorted.filter(t => t.type === 'Income').reduce((s, t) => s + t.amount, 0);
      const totalExpense = sorted.filter(t => t.type === 'Expense').reduce((s, t) => s + t.amount, 0);

      const rows = [
        ['Project',     project.name],
        ['Type',        project.type],
        ['Exported At', new Date().toLocaleString()],
        [],
        ['Date', 'Type', 'Category', 'Amount', 'Note'],
        ...sorted.map(tx => [
          new Date(tx.date).toLocaleDateString('en-GB'),
          tx.type,
          catMap[tx.categoryId] || 'Unknown',
          tx.amount,
          tx.note || '',
        ]),
        [],
        ['Total Income',  '', '', totalIncome,                 ''],
        ['Total Expense', '', '', totalExpense,                ''],
        ['Balance',       '', '', totalIncome - totalExpense,  ''],
      ];

      const csv = rows.map(r => r.map(csvCell).join(',')).join('\r\n');
      downloadFile(
        csv,
        `${project.name.replace(/[^a-z0-9]/gi, '_')}_Report_${dateStamp()}.csv`,
        'text/csv;charset=utf-8;'
      );
      showToast('CSV downloaded!');
    } catch (e) {
      showToast('Export failed: ' + e.message, 'danger');
    }
  }

  async function importJSON() {
    const file = $('importFile')?.files?.[0];
    if (!file) { showToast('Please select a backup file.', 'danger'); return; }

    try {
      const text   = await file.text();
      const backup = JSON.parse(text);

      if (!Array.isArray(backup.projects) || !Array.isArray(backup.categories) || !Array.isArray(backup.transactions)) {
        showToast('Invalid backup file — missing required data.', 'danger');
        return;
      }

      const exportedAt = backup.exportedAt
        ? new Date(backup.exportedAt).toLocaleString()
        : 'unknown date';

      if (!confirm(
        `Restore backup from ${exportedAt}?\n\n` +
        `This will replace ALL current data:\n` +
        `• ${backup.projects.length} projects\n` +
        `• ${backup.transactions.length} transactions\n` +
        `• ${backup.categories.length} categories`
      )) return;

      await FinanceDB.clearStore('transactions');
      await FinanceDB.clearStore('categories');
      await FinanceDB.clearStore('projects');

      // Normalise IDs to strings (handles both old IndexedDB numeric & new Firestore string IDs)
      for (const item of backup.projects)
        await FinanceDB.putItem('projects', { ...item, id: String(item.id) });
      for (const item of backup.categories)
        await FinanceDB.putItem('categories', { ...item, id: String(item.id) });
      for (const item of backup.transactions)
        await FinanceDB.putItem('transactions', {
          ...item,
          id:         String(item.id),
          projectId:  String(item.projectId),
          categoryId: String(item.categoryId),
        });

      showToast(
        `Restored: ${backup.projects.length} projects, ${backup.transactions.length} transactions.`
      );
      location.hash = '#/dashboard';
    } catch (e) {
      showToast(
        'Restore failed: ' + (e instanceof SyntaxError ? 'Invalid JSON file.' : e.message),
        'danger'
      );
    }
  }

  /* ══════════════════════════════════════════════════════════════════════════
     AUTH
  ══════════════════════════════════════════════════════════════════════════ */

  function showApp(user) {
    const ls = $('login-screen');
    if (ls) ls.classList.add('d-none');
    const userInfo = $('userInfo');
    if (userInfo) userInfo.style.removeProperty('display');
    const emailEl = $('userEmail');
    if (emailEl) emailEl.textContent = user.email;
  }

  function showLoginScreen() {
    content().innerHTML = '';
    const ls = $('login-screen');
    if (ls) ls.classList.remove('d-none');
    const userInfo = $('userInfo');
    if (userInfo) userInfo.style.setProperty('display', 'none', 'important');
    setTimeout(() => $('loginEmail')?.focus(), 120);
  }

  async function handleLogin() {
    const email    = ($('loginEmail')?.value   || '').trim();
    const password = ($('loginPassword')?.value || '');
    const errorEl  = $('loginError');
    const spinner  = $('loginSpinner');
    const btn      = $('loginBtn');

    if (!email || !password) {
      if (errorEl) { errorEl.textContent = 'Please enter your email and password.'; errorEl.classList.remove('d-none'); }
      return;
    }

    if (errorEl) errorEl.classList.add('d-none');
    if (spinner) spinner.classList.remove('d-none');
    if (btn)     btn.disabled = true;

    try {
      await fbAuth.signInWithEmailAndPassword(email, password);
      // onAuthStateChanged fires automatically → showApp() is called
    } catch (err) {
      const known = ['auth/invalid-credential', 'auth/wrong-password', 'auth/user-not-found'];
      const msg   = known.includes(err.code) ? 'Invalid email or password.' : 'Sign in failed. Please try again.';
      if (errorEl) { errorEl.textContent = msg; errorEl.classList.remove('d-none'); }
    } finally {
      if (spinner) spinner.classList.add('d-none');
      if (btn)     btn.disabled = false;
    }
  }

  async function logout() {
    if (!confirm('Sign out?')) return;
    content().innerHTML = '';
    await fbAuth.signOut();
    // onAuthStateChanged fires with null → showLoginScreen()
  }

  /* ══════════════════════════════════════════════════════════════════════════
     INIT
  ══════════════════════════════════════════════════════════════════════════ */

  async function init() {
    // Register service worker
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.register('./sw.js').catch(err => {
        console.warn('Service Worker registration failed:', err);
      });
    }

    // PWA install prompt (Android Chrome / Edge)
    let deferredInstallPrompt = null;
    window.addEventListener('beforeinstallprompt', e => {
      e.preventDefault();
      deferredInstallPrompt = e;
      const banner = $('install-banner');
      if (banner) banner.classList.remove('d-none');
    });

    const installBtn    = $('installBtn');
    const dismissBtn    = $('dismissInstall');
    const installBanner = $('install-banner');

    if (installBtn) {
      installBtn.addEventListener('click', async () => {
        if (!deferredInstallPrompt) return;
        deferredInstallPrompt.prompt();
        const { outcome } = await deferredInstallPrompt.userChoice;
        deferredInstallPrompt = null;
        if (installBanner) installBanner.classList.add('d-none');
        if (outcome === 'accepted') showToast('App installed!');
      });
    }

    if (dismissBtn && installBanner) {
      dismissBtn.addEventListener('click', () => installBanner.classList.add('d-none'));
    }

    // Login form event handlers
    $('loginBtn')?.addEventListener('click', handleLogin);
    $('loginPassword')?.addEventListener('keydown', e => { if (e.key === 'Enter') handleLogin(); });
    $('loginEmail')?.addEventListener('keydown',    e => { if (e.key === 'Enter') $('loginPassword')?.focus(); });

    // Hash-based routing (route() is a no-op when not authenticated)
    window.addEventListener('hashchange', route);

    // Firebase auth state listener — drives the entire app lifecycle
    fbAuth.onAuthStateChanged(async user => {
      if (user) {
        showApp(user);
        try { await FinanceDB.seedDefaults(); } catch (e) { console.warn('Seed failed:', e); }
        if (!location.hash || location.hash === '#' || location.hash === '#/') {
          location.hash = '#/dashboard';
        } else {
          await route();
        }
      } else {
        showLoginScreen();
      }
    });
  }

  /* ══════════════════════════════════════════════════════════════════════════
     PUBLIC API
  ══════════════════════════════════════════════════════════════════════════ */

  return {
    init,
    // Projects
    addProject,
    editProject,
    deleteProject,
    // Transactions
    addTransaction,
    editTransaction,
    deleteTransaction,
    showTransactionDetail,
    refreshTxCategories,
    applyTxFilters,
    txGoToPage,
    // Categories
    addCategory,
    editCategory,
    deleteCategory,
    // Backup
    exportJSON,
    exportCSV,
    importJSON,
    // Auth
    logout,
  };
})();

// Bootstrap the app
document.addEventListener('DOMContentLoaded', App.init);
