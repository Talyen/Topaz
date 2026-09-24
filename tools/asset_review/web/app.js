const state = { items: [], choices: {}, page: 0, pageSize: 24 };
const $ = id => document.getElementById(id);
const statusName = status => status === 'keep' ? 'Keep' : status === 'maybe' ? 'Maybe' : status === 'archive' ? 'Archive' : 'Unreviewed';
const escapeHtml = value => String(value).replace(/[&<>"']/g, character => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]));

function filtered() {
  const pack = $('pack').value, kind = $('kind').value, status = $('status').value, query = $('search').value.trim().toLowerCase();
  return state.items.filter(item =>
    (pack === 'all' || item.pack === pack) && (kind === 'all' || item.kind === kind) &&
    (status === 'all' || (state.choices[item.guid]?.status || 'pending') === status) &&
    (!query || `${item.name} ${item.path}`.toLowerCase().includes(query)));
}

function pageItems() { return filtered().slice(state.page * state.pageSize, (state.page + 1) * state.pageSize); }

function summary() {
  const counts = {keep:0, maybe:0, archive:0};
  for (const item of state.items) if (counts[state.choices[item.guid]?.status] !== undefined) counts[state.choices[item.guid].status]++;
  $('summary').innerHTML = `<div class="stat"><b>${state.items.length}</b><span>Total</span></div>` +
    `<div class="stat keep"><b>${state.items.length - counts.maybe - counts.archive}</b><span>Available</span></div>` +
    `<div class="stat maybe"><b>${counts.maybe}</b><span>Maybe</span></div>` +
    `<div class="stat archive"><b>${counts.archive}</b><span>Archive</span></div>` +
    `<div class="stat replace"><b>${state.items.filter(item => item.referenced && state.choices[item.guid]?.status === 'archive').length}</b><span>Replace</span></div>`;
}

function card(item) {
  const choice = state.choices[item.guid] || {};
  const visual = item.preview ? `<img src="${item.preview}" alt="Preview of ${escapeHtml(item.name)}" loading="lazy">` :
    `<div class="placeholder"><i>${item.kind === 'Animation' ? '↝' : '◇'}</i><span>${item.kind === 'Animation' ? 'Motion preview pending' : 'Preview pending'}</span></div>`;
  return `<article class="card" data-guid="${item.guid}" tabindex="0">
    <button class="thumb" type="button" data-action="view" aria-label="Enlarge ${escapeHtml(item.name)}">${visual}</button>
    <div class="card-body"><div class="tags"><span>${escapeHtml(item.pack)} · ${item.kind}</span>${item.referenced ? `<span class="reference">${choice.status === 'archive' ? 'Needs replacement' : 'Referenced'}</span>` : ''}</div>
    <h4>${escapeHtml(item.name)}</h4><div class="path" title="${escapeHtml(item.path)}">${escapeHtml(item.previewClip ? `Motion sample: ${item.previewClip}` : `${item.family} / ${item.path.split('/').at(-1)}`)}</div>
    <div class="decisions">${['keep','maybe','archive'].map(value => `<button type="button" class="${value} ${choice.status === value ? 'active' : ''}" data-action="${value}" aria-pressed="${choice.status === value}">${statusName(value)}</button>`).join('')}</div>
    <input class="note" aria-label="Note for ${escapeHtml(item.name)}" maxlength="500" placeholder="Optional note" value="${escapeHtml(choice.note || '')}"></div></article>`;
}

function render() {
  const results = filtered(), pages = Math.max(1, Math.ceil(results.length / state.pageSize));
  state.page = Math.min(state.page, pages - 1);
  const items = pageItems();
  $('result-title').textContent = $('pack').value === 'all' ? 'All assets' : $('pack').value;
  $('result-count').textContent = `${results.length} assets · ${results.filter(item => !state.choices[item.guid]).length} unmarked and available`;
  $('grid').innerHTML = items.length ? items.map(card).join('') : '<div class="empty">No assets match these filters.</div>';
  $('page-label').textContent = `Page ${state.page + 1} of ${pages}`;
  $('previous').disabled = state.page === 0;
  $('next').disabled = state.page >= pages - 1;
  $('batch').disabled = !items.length;
  summary();
}

let saveQueue = Promise.resolve();
function save(changes) {
  saveQueue = saveQueue.then(() => persist(changes), () => persist(changes));
  return saveQueue;
}

async function persist(changes) {
  $('save-state').textContent = 'Saving…';
  try {
    const response = await fetch('/api/decisions', {method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify({changes})});
    const payload = await response.json();
    if (!response.ok) throw new Error(payload.error || 'Could not save');
    state.choices = payload.choices;
    if (payload.catalogChanged) {
      const refreshed = await (await fetch('/api/catalog')).json();
      state.items = refreshed.items;
      state.choices = refreshed.choices;
    }
    $('save-state').textContent = 'Choices saved';
    render();
  } catch (error) {
    $('save-state').textContent = `Save failed: ${error.message}`;
  }
}

function openViewer(item) {
  const images = item.frames.length ? item.frames : item.preview ? [item.preview] : [];
  $('viewer-content').innerHTML = images.length ? `<img id="viewer-image" src="${images[0]}" alt="${escapeHtml(item.name)}"><div class="viewer-caption">${escapeHtml(item.name)}<small>${escapeHtml(item.previewClip ? `Representative clip: ${item.previewClip} · ` : '')}${escapeHtml(item.path)}</small></div>` :
    `<div class="placeholder" style="height:300px">Preview pending</div><div class="viewer-caption">${escapeHtml(item.name)}<small>${escapeHtml(item.path)}</small></div>`;
  $('viewer').showModal();
  if (images.length > 1) {
    let index = 0;
    const timer = setInterval(() => { if (!$('viewer').open) { clearInterval(timer); return; } index = (index + 1) % images.length; $('viewer-image').src = images[index]; }, 180);
  }
}

async function start() {
  const response = await fetch('/api/catalog');
  if (!response.ok) throw new Error('Could not load catalog');
  const data = await response.json();
  state.items = data.items;
  state.choices = data.choices;
  const packs = [...new Set(state.items.map(item => item.pack))].sort();
  $('pack').innerHTML = '<option value="all">All packs</option>' + packs.map(pack => `<option value="${escapeHtml(pack)}">${escapeHtml(pack)}</option>`).join('');
  $('pack').value = packs.includes('Forest') ? 'Forest' : 'all';
  $('save-state').textContent = 'Choices saved';
  render();
}

for (const id of ['pack','kind','status','search']) $(id).addEventListener(id === 'search' ? 'input' : 'change', () => { state.page = 0; render(); });
$('previous').addEventListener('click', () => { state.page--; render(); window.scrollTo(0, 300); });
$('next').addEventListener('click', () => { state.page++; render(); window.scrollTo(0, 300); });
$('grid').addEventListener('click', event => {
  const action = event.target.closest('[data-action]')?.dataset.action;
  const cardElement = event.target.closest('[data-guid]');
  if (!action || !cardElement) return;
  const item = state.items.find(candidate => candidate.guid === cardElement.dataset.guid);
  if (action === 'view') { openViewer(item); return; }
  const previous = state.choices[item.guid] || {};
  save([{guid:item.guid, status:previous.status === action ? null : action, note:cardElement.querySelector('.note').value}]);
});
$('grid').addEventListener('change', event => {
  if (!event.target.classList.contains('note')) return;
  const guid = event.target.closest('[data-guid]').dataset.guid;
  const choice = state.choices[guid];
  if (choice) save([{guid, status:choice.status, note:event.target.value}]);
});
$('grid').addEventListener('keydown', event => {
  if (event.target.matches('input')) return;
  const status = {'1':'keep','2':'maybe','3':'archive'}[event.key];
  const guid = event.target.closest('[data-guid]')?.dataset.guid;
  if (status && guid) { event.preventDefault(); save([{guid,status,note:state.choices[guid]?.note || ''}]); }
});
$('close-viewer').addEventListener('click', () => $('viewer').close());
$('viewer').addEventListener('click', event => { if (event.target === $('viewer')) $('viewer').close(); });
$('batch').addEventListener('click', () => { $('batch-count').textContent = pageItems().length; $('batch-dialog').showModal(); });
$('batch-dialog').addEventListener('close', () => {
  const decision = $('batch-dialog').returnValue;
  if (['keep','maybe','archive'].includes(decision)) save(pageItems().map(item => ({guid:item.guid,status:decision,note:state.choices[item.guid]?.note || ''})));
});
start().catch(error => { $('save-state').textContent = error.message; $('grid').innerHTML = `<div class="empty">${escapeHtml(error.message)}</div>`; });
