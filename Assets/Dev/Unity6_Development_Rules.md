# Unity 6 Development Rules (C#)

Este documento define padrões obrigatórios para projetos Unity 6 em C#.

Estas regras devem ser seguidas sempre, a menos que explicitamente definido o contrário.

---

## 1. Princípios Centrais

### 1.1 SOLID (adaptado para Unity)

- Siga SOLID sempre que possível.
- Uma classe = uma responsabilidade clara.
- Prefira composição sobre herança.
- Use interfaces e abstrações sempre que possível.

Regra prática:
- MonoBehaviour não deve virar God Class.
- Separe input, movimento, combate, animação, áudio, UI e dados.

---

### 1.2 DRY

- Nunca duplique lógica.
- Extraia comportamento compartilhado para:
  - Classes puras
  - Serviços
  - Componentes reutilizáveis
  - ScriptableObjects

---

## 2. Tipagem e API Pública

- Evite var quando prejudicar clareza.
- Prefira tipos explícitos.
- Use padrões Try.

### Nullability
- Valide referências em Awake ou OnValidate.
- Nunca deixe NullReference acontecer silenciosamente.

---

## 3. Orientação a Objetos

### Component-Based + Classes Puras
- MonoBehaviour apenas para ciclo de vida e integração.
- Lógica deve ficar em classes puras.

### ScriptableObject
- Usado apenas para dados.
- Regras ficam em código.

---

## 4. Fluxo de Controle

- Prefira early return.
- Nunca escreva condições gigantes direto no if.

---

## 5. Convenções de Nomes

- Classes/Métodos/Propriedades: PascalCase
- Campos privados: _camelCase
- Constantes: UPPER_SNAKE_CASE
- Interfaces: IExample

Sem abreviações.

---

## 6. Cabeçalho do Arquivo

Todo arquivo deve iniciar com:

```csharp
// Purpose: Describe what this class does.
```

---

## 7. Formatação

- Limite recomendado: 100–120 caracteres.
- Use editorconfig.
- Clareza > tamanho.

---

## 8. Números Mágicos

Nunca use números soltos.
Extraia para constantes ou ScriptableObjects.

---

## 9. Constantes e Configuração

Use const, static readonly ou ScriptableObjects.

---

## 10. Tratamento de Erros

- Nunca engula exceções.
- Sempre logue com contexto.
- Evite spam de log em Update.

---

## 11. Ordem de Usings

1. System
2. Unity
3. Pacotes externos
4. Projeto

Separados por linha em branco.

---

## 12. Final do Arquivo

Sempre terminar com exatamente uma linha em branco.

---

## 13. Regras Unity

### Ciclo de vida
- FixedUpdate para física.
- Evite lógica pesada em Update.

### Performance
- Evite new e LINQ por frame.
- Cache referências.
- Use object pooling.

### Inspector
- SerializeField para dependências.
- OnValidate para validar estado.

### Arquitetura
- Componentes pequenos.
- Prefira eventos a polling.

---

## 14. Filosofia

Simples > Esperto  
Explícito > Implícito  
Legível > Curto  

Código deve ser previsível, claro e fácil de manter.
