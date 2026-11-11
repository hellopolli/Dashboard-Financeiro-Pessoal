# Painel Financeiro Pessoal - Dashboard Interativo 💰

Uma aplicação web intuitiva que permite aos usuários registrar receitas e despesas, gerenciando sua saúde financeira em um painel interativo com gráficos e relatórios detalhados.

---

## 🎯 Problema e Usuário-alvo
* **Problema:** Pessoas que desejam controlar suas finanças pessoais, mas utilizam planilhas manuais complexas e sem visão clara e visual de para onde vai o dinheiro.
* **Usuário-alvo:** Pessoas físicas que desejam acompanhar gastos por categoria e comparar meses de forma simples, visual e automatizada.

## 🚀 Funcionalidades Implementadas

* **Autenticação Segura:** Cadastro, Login e Recuperação de Senha (com hash seguro PBKDF2 e autenticação via Cookies/Claims).
* **Gestão de Transações:** Registro de receitas e despesas com valores, datas e categorias personalizadas.
* **Dashboard Interativo:** Gráficos mostrando a distribuição de gastos por categoria e comparativos mensais.
* **Exportação de Relatórios:** Geração de relatórios em PDF para acompanhamento offline.
* **Perfis Personalizados:** Escolha de moeda (BRL, USD, EUR) e seleção de planos (Básico/Premium).

## 🛠️ Tecnologias Utilizadas (Stack Atual)

* **Back-end:** C# com ASP.NET Core Web API
* **Banco de Dados:** SQLite (via Entity Framework Core)
* **Front-end:** HTML5, CSS3, JavaScript (Vanilla)
* **Design & Prototipagem:** Figma
* **Organização & Controle:** Git/GitHub, Trello (Kanban)

---

## 💡 Decisões Técnicas e Alterações no Escopo Original

Durante o ciclo de desenvolvimento, a equipe tomou decisões arquiteturais estratégicas para otimizar a entrega, a segurança e a usabilidade do projeto para fins de avaliação:

### 1. Mudança de SQL Server/PostgreSQL para SQLite
* **Motivo original:** Previa-se um SGBD robusto rodando em um servidor dedicado.
* **A mudança:** Optamos pelo **SQLite**. Por ser um banco de dados embutido num único arquivo (`.db`), ele torna o projeto 100% *"plug and play"*. Os avaliadores e usuários não precisam instalar servidores complexos (como SQL Server) ou configurar *Connection Strings* nas suas máquinas. Basta clonar o repositório e rodar o projeto que o banco é gerado e lido automaticamente.

### 2. Mocking de E-mail e Separação de Responsabilidades nas Rotas
* **Motivo original:** Uso de um serviço real de disparo de e-mails (como Sendinblue/Brevo).
* **A mudança:** O GitHub bloqueia chaves de API expostas no código (*Secret Scanning*). Para não exigir configurações de variáveis de ambiente por parte dos avaliadores, o sistema de e-mail foi simulado. 
* **Arquitetura do Fluxo:** Para garantir o funcionamento perfeito sem conflitos de cache no navegador e manter o código limpo, a equipe aplicou a *Separação de Responsabilidades (Separation of Concerns)* no front-end, dividindo o fluxo de recuperação em etapas distintas:
  1. **Solicitação (`esqueci.html`):** Interface exclusiva para o usuário informar o e-mail.
  2. **Geração Segura (Back-end C#):** A API valida o usuário, gera um token criptografado e imprime o link de recuperação destacado em amarelo no terminal local.
  3. **Redefinição (`nova-senha.html`):** O link do terminal direciona o usuário para uma rota isolada que extrai o token da URL e processa o envio seguro da nova senha para o banco de dados.

### 3. Adições Extras (Além do Escopo)
* Embora o escopo inicial definisse que não teríamos múltiplas moedas, a equipe conseguiu ir além e implementou com sucesso a personalização de moedas no perfil do usuário (BRL, USD, EUR), bem como a estrutura para gerenciamento de Planos ativos.

---

## ⚙️ Como Executar o Projeto (Plug and Play)

Por utilizarmos SQLite e simulação de e-mails, rodar este projeto é extremamente simples:

1. Clone o repositório:
   ```bash
   git clone [https://github.com/hellopolli/Dashboard-Financeiro-Pessoal.git](https://github.com/hellopolli/Dashboard-Financeiro-Pessoal.git)