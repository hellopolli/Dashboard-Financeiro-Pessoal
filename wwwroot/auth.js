const showNotice = (id, message, type = 'error') => {
  const notice = document.getElementById(id);
  if (!notice) return;
  notice.textContent = message;
  notice.className = `notice show ${type}`;
};

const send = async (path, payload) => {
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });
  const result = await response.json().catch(() => ({}));
  if (!response.ok) throw new Error(result.message || 'Não foi possível concluir. Tente novamente.');
  return result;
};

document.getElementById('login-form')?.addEventListener('submit', async event => {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  const button = form.querySelector('button[type="submit"]');
  button.disabled = true;
  try {
    const user = await send('/api/auth/login', {
      email: form.email.value,
      senha: form.senha.value
    });
    sessionStorage.setItem('usuario', JSON.stringify(user));
    window.location.href = '/plano.html';
  } catch (error) {
    showNotice('login-notice', error.message);
  } finally {
    button.disabled = false;
  }
});

document.getElementById('register-form')?.addEventListener('submit', async event => {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  if (form.senha.value !== form.confirmarSenha.value) {
    showNotice('register-notice', 'As senhas não coincidem.');
    form.confirmarSenha.focus();
    return;
  }
  const button = form.querySelector('button[type="submit"]');
  button.disabled = true;
  try {
    await send('/api/auth/register', {
      nome: form.nome.value,
      email: form.email.value,
      senha: form.senha.value
    });
    window.location.href = '/login.html?cadastro=sucesso';
  } catch (error) {
    showNotice('register-notice', error.message);
  } finally {
    button.disabled = false;
  }
});

document.getElementById('forgot-form')?.addEventListener('submit', async event => {
  event.preventDefault();
  const form = event.currentTarget;
  if (!form.reportValidity()) return;
  const button = form.querySelector('button[type="submit"]');
  button.disabled = true;
  try {
    await send('/api/auth/forgot-password', { email: form.email.value });
    window.location.href = '/confirmacao-senha.html';
  } catch (error) {
    showNotice('forgot-notice', error.message);
  } finally {
    button.disabled = false;
  }
});

document.getElementById('choose-plan')?.addEventListener('click', () => {
  const user = sessionStorage.getItem('usuario');
  if (!user) {
    window.location.href = '/cadastro.html?plano=individual';
    return;
  }
  const message = document.getElementById('plan-message');
  message.textContent = 'Plano selecionado. A contratação estará disponível quando o pagamento for integrado.';
});

if (window.location.search.includes('cadastro=sucesso')) {
  showNotice('login-notice', 'Conta criada com sucesso. Entre para continuar.', 'success');
}
