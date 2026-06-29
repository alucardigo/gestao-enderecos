// Autopreenchimento por CEP. Vanilla JS, sem dependências.
// Ao completar 8 dígitos no campo CEP, consulta o endpoint interno, preenche os campos
// geográficos e move o foco para "Número". Em qualquer falha, libera o preenchimento manual.
(function () {
    "use strict";

    const cep = document.getElementById("cep");
    if (!cep) {
        return;
    }

    const status = document.getElementById("cepStatus");
    const numero = document.getElementById("numero");
    const campos = {
        logradouro: document.getElementById("logradouro"),
        bairro: document.getElementById("bairro"),
        cidade: document.getElementById("cidade"),
        uf: document.getElementById("uf"),
    };

    cep.addEventListener("input", function () {
        const digitos = cep.value.replace(/\D/g, "").slice(0, 8);
        cep.value = digitos.length > 5 ? `${digitos.slice(0, 5)}-${digitos.slice(5)}` : digitos;
        if (digitos.length === 8) {
            buscar(digitos);
        }
    });

    async function buscar(digitos) {
        definirStatus("Buscando endereço…", "text-muted");
        try {
            const resposta = await fetch(`/Enderecos/BuscarCep?cep=${digitos}`, {
                headers: { Accept: "application/json" },
            });

            if (!resposta.ok) {
                definirStatus("CEP não encontrado. Preencha manualmente.", "text-warning");
                return;
            }

            const dados = await resposta.json();
            campos.logradouro.value = dados.logradouro || "";
            campos.bairro.value = dados.bairro || "";
            campos.cidade.value = dados.cidade || "";
            campos.uf.value = dados.uf || "";
            definirStatus("Endereço preenchido automaticamente.", "text-success");
            numero?.focus();
        } catch {
            definirStatus("Não foi possível consultar o CEP agora. Preencha manualmente.", "text-warning");
        }
    }

    function definirStatus(mensagem, classe) {
        if (status) {
            status.textContent = mensagem;
            status.className = `form-text ${classe}`;
        }
    }
})();
