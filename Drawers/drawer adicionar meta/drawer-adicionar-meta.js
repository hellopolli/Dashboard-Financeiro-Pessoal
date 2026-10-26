function initMetaDrawerEvents() {
  const drawer = document.getElementById('metaDrawer');
  const overlay = document.getElementById('drawerOverlay');
  const btnOpen = document.getElementById('btnOpenDrawer');
  const btnCancel = document.getElementById('btnCancel');
  const btnSave = document.getElementById('btnSave');
  const form = document.getElementById('metaForm');

  const inputNome = document.getElementById('nomeMeta');
  const inputValorDesejado = document.getElementById('valorDesejado');
  const inputValorGuardado = document.getElementById('valorGuardado');
  const inputPrazo = document.getElementById('prazoMeta');

  if (!drawer || !btnOpen) return;

  // Abrir Drawer
  btnOpen.addEventListener('click', () => {
    drawer.classList.add('open');
    if (overlay) overlay.classList.add('active');
  });

  // Fechar Drawer
  function closeDrawer() {
    drawer.classList.remove('open');
    if (overlay) overlay.classList.remove('active');
    if (form) form.reset();
    clearAllErrors();
  }

  if (btnCancel) btnCancel.addEventListener('click', closeDrawer);
  if (overlay) overlay.addEventListener('click', closeDrawer);

  // Função para aplicar máscara monetária em R$
  function applyCurrencyMask(inputElement, fieldId) {
    if (!inputElement) return;
    inputElement.addEventListener('input', (e) => {
      let value = e.target.value.replace(/\D/g, '');
      if (value === '') {
        e.target.value = '';
        return;
      }
      value = (parseInt(value, 10) / 100).toFixed(2);
      value = value.replace('.', ',');
      value = value.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
      e.target.value = `R$ ${value}`;
      hideError(fieldId);
    });
  }

  // Aplicação da máscara de moeda nos campos financeiros
  applyCurrencyMask(inputValorDesejado, 'valorDesejado');
  applyCurrencyMask(inputValorGuardado, 'valorGuardado');

  // Controle de Erros Visuais
  function showError(fieldId) {
    const errorSpan = document.getElementById(`error-${fieldId}`);
    const inputElem = document.getElementById(fieldId);
    if (errorSpan) errorSpan.style.display = 'inline';
    if (inputElem) inputElem.classList.add('input-error');
  }

  function hideError(fieldId) {
    const errorSpan = document.getElementById(`error-${fieldId}`);
    const inputElem = document.getElementById(fieldId);
    if (errorSpan) errorSpan.style.display = 'none';
    if (inputElem) inputElem.classList.remove('input-error');
  }

  function clearAllErrors() {
    ['nomeMeta', 'valorDesejado', 'valorGuardado', 'prazoMeta'].forEach(hideError);
  }

  ['nomeMeta', 'valorDesejado', 'valorGuardado', 'prazoMeta'].forEach(id => {
    const elem = document.getElementById(id);
    if (elem) {
      elem.addEventListener('input', () => hideError(id));
      elem.addEventListener('change', () => hideError(id));
    }
  });

  // Validação ao Clicar em Salvar
  if (btnSave) {
    btnSave.addEventListener('click', () => {
      clearAllErrors();
      let isValid = true;

      if (!inputNome || !inputNome.value.trim()) {
        showError('nomeMeta');
        isValid = false;
      }

      if (!inputValorDesejado || !inputValorDesejado.value.trim()) {
        showError('valorDesejado');
        isValid = false;
      }

      if (!inputValorGuardado || !inputValorGuardado.value.trim()) {
        showError('valorGuardado');
        isValid = false;
      }

      if (!inputPrazo || !inputPrazo.value.trim()) {
        showError('prazoMeta');
        isValid = false;
      }

      if (isValid) {
        const payload = {
          nomeMeta: inputNome.value.trim(),
          valorDesejado: inputValorDesejado.value,
          valorGuardado: inputValorGuardado.value,
          prazoMeta: inputPrazo.value
        };

        console.log('Payload da Meta pronto para envio ao Banco de Dados:', JSON.stringify(payload, null, 2));
        alert('Meta criada com sucesso e enviada para o banco de dados!');
        closeDrawer();
      }
    });
  }
}