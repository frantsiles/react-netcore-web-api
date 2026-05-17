---
name: premortem
description: "Ejecuta un premortem sobre cualquier plan, lanzamiento, producto, contratación, estrategia o decisión. Asume que ya falló 6 meses después y trabaja hacia atrás para encontrar todos los motivos. Produce un plan revisado con los puntos ciegos expuestos. DISPARADORES OBLIGATORIOS: 'premortem esto', 'premortem mi', 'ejecuta un premortem', 'qué podría matar esto', 'prueba de estrés este plan', 'qué me estoy perdiendo aquí', 'encuentra los puntos ciegos'. DISPARADORES FUERTES: 'qué podría salir mal', 'me estoy perdiendo algo', 'hazle agujeros a esto', 'dónde va a romperse esto', 'abogado del diablo'. NO disparar en solicitudes simples de retroalimentación, preguntas factuales o solicitudes al Consejo LLM. SÍ disparar cuando alguien tiene un plan o compromiso donde el coste de equivocarse es alto."
---

# Premortem

Un premortem es lo contrario de un postmortem. En lugar de averiguar qué salió mal después de que algo falla, imaginas que ya falló y averiguas por qué antes de empezar.

El método proviene del psicólogo Gary Klein. Lo publicó en Harvard Business Review. Daniel Kahneman (el psicólogo ganador del Premio Nobel detrás de "Pensar rápido, pensar despacio") lo llamó su técnica más valiosa para la toma de decisiones. Google, Goldman Sachs y Procter & Gamble lo usan antes de grandes decisiones.

La idea clave: cuando preguntas a la gente "¿qué podría salir mal?" dan respuestas cautelosas y ambiguas. Cuando dices "esto ya falló, dime por qué", el cerebro cambia al modo narrativo y genera razones mucho más específicas, creativas y honestas. Investigadores de Wharton y Cornell llamaron a esto "retrospectiva prospectiva" y descubrieron que aumenta significativamente la capacidad de identificar causas de resultados futuros.

Por qué importa esto para las decisiones asistidas por IA: Claude tiende a respuestas amables y optimistas. Si preguntas "¿es este un buen plan?" encontrará razones para decir que sí. El premortem rompe este patrón al forzar el encuadre en "esto está muerto, explica cómo murió". Claude deja de buscar razones por las que tu plan funcionará y empieza a explicar cómo se desmoronó.

---

## cuándo ejecutar un premortem

Buenos objetivos para un premortem:
- Un producto o funcionalidad que estás a punto de construir
- Un plan de lanzamiento con dinero o reputación en juego
- Un cambio de precio o modelo de negocio
- Una contratación que estás a punto de hacer
- Un pivote de estrategia o posicionamiento
- Una alianza o acuerdo que estás evaluando
- Cualquier compromiso donde el coste de equivocarse es alto

Malos objetivos para un premortem:
- Ideas vagas sin ningún plan concreto todavía (ayúdalos a planificar primero, luego haz el premortem)
- Preguntas con una sola respuesta correcta (simplemente respóndelas)
- Solicitudes de retroalimentación creativa sobre un borrador (eso es edición, no un premortem)
- Decisiones que ya están tomadas e irreversibles (un premortem solo es útil cuando aún puedes cambiar de rumbo)

**Desambiguación con el Consejo LLM:** si el usuario parece querer múltiples perspectivas sobre una decisión *ahora mismo* en lugar de un análisis de fallos *futuros*, sugiere el Consejo LLM en su lugar. Si hay ambigüedad genuina ("¿cuál de los dos quieres?"), pregunta antes de proceder. El premortem envía a Claude al futuro donde la decisión ya falló; el Consejo evalúa opciones en el presente. Son mecanismos distintos con resultados distintos.

---

## recopilación de contexto (el mínimo necesario)

Un premortem es tan bueno como el contexto sobre el que se ejecuta. La información vaga produce escenarios de fallo vagos que no ayudan a nadie. Antes de ejecutar el premortem, necesitas alcanzar un umbral mínimo de contexto.

### paso 1: buscar contexto existente

Antes de preguntar nada al usuario, busca contexto que ya esté disponible:

**A. La conversación actual.** El usuario puede haber estado discutiendo un plan, un lanzamiento, un producto o una decisión antes en esta sesión. Lee la conversación y extrae lo que sea relevante.

**B. El espacio de trabajo.** Escanea rápidamente en busca de archivos que puedan contener contexto relevante:
- `CLAUDE.md` o `claude.md` (contexto empresarial, preferencias, restricciones)
- Cualquier carpeta `memory/` (perfiles de audiencia, detalles del negocio, decisiones pasadas)
- Archivos que el usuario referenciara o adjuntara explícitamente
- Cualquier archivo de proyecto, briefs o planes relacionados con lo que se está sometiendo al premortem

Usa `Glob` y llamadas rápidas a `Read`. No dediques más de 30 segundos a esto. Buscas los archivos clave que anclarán los escenarios de fallo en la realidad.

### paso 2: evaluar la suficiencia del contexto

Después de escanear, comprueba si tienes suficiente para ejecutar un premortem útil. Necesitas tres cosas obligatorias y, si están disponibles, contexto enriquecido:

**Contexto mínimo obligatorio (los tres):**

1. **¿Qué es?** — Una comprensión clara de lo que se está sometiendo al premortem (un producto, un lanzamiento, una contratación, un cambio de precio, una estrategia). Debes ser capaz de describírselo al usuario en una frase.

2. **¿Para quién es / a quién afecta?** — La audiencia, el cliente, el equipo, las partes interesadas. Los escenarios de fallo dependen en gran medida de quién está involucrado.

3. **¿Cómo es el éxito?** — ¿Qué resultado espera el usuario? El fallo se define invirtiendo el éxito. Si no sabes qué significa el éxito, no puedes definir qué significa el fracaso.

**Contexto enriquecido (opcional, mejora la calidad sin ser obligatorio):**
- Competidores o alternativas existentes que el usuario ya conoce
- Restricciones de tiempo o recursos relevantes
- Historial de decisiones similares (qué funcionó antes, qué no)
- Suposiciones que el usuario ya sabe que está haciendo

Si tienes el contexto enriquecido, úsalo para anclar mejor los modos de fallo en la realidad. Si no está disponible, no lo solicites a menos que sea crítico para el análisis.

### paso 3: completar las lagunas de forma conversacional

Si tienes los tres obligatorios, procede inmediatamente al premortem. No hagas preguntas innecesarias.

Si te falta uno o más, pregunta primero por la pieza más importante que falta. Una pregunta a la vez. Evalúa después de cada respuesta si ahora tienes suficiente. Sigue preguntando hasta alcanzar el umbral, pero nunca preguntes más de lo necesario.

Ejemplos de preguntas de contexto enfocadas:
- "¿Qué es exactamente lo que estás a punto de lanzar/construir/decidir?" (si no sabes qué es)
- "¿Para quién es esto?" (si conoces el plan pero no la audiencia)
- "¿Cómo sería una victoria para esto?" (si conoces el plan y la audiencia pero no los criterios de éxito)

El objetivo es alcanzar el mínimo lo más rápido posible sin hacer que el usuario sienta que está rellenando un formulario. Conversacional, no interrogativo. Si puedes inferir una respuesta del contexto, hazlo en lugar de preguntar.

---

## cómo funciona una sesión de premortem

### paso 1: establecer el encuadre

Después de recopilar suficiente contexto, establece el encuadre del premortem explícitamente. Algo como:

"Bien, tengo suficiente contexto. Vamos a ejecutar el premortem. La premisa es: han pasado 6 meses. [El plan/lanzamiento/decisión] ha fallado. Está hecho. Miramos hacia atrás intentando entender qué salió mal."

Este encuadre importa. Cambia el modo de "evalúa este plan" (que desencadena respuestas complacientes) a "explica por qué murió esto" (que desencadena una identificación honesta y específica de los fallos).

### paso 2: generar razones de fallo (premortem en bruto)

Ejecuta el premortem en bruto como un análisis único y completo. Sin categorías prefijadas, sin lentes, sin restricciones. Solo el método Klein básico:

"Este plan ha fallado 6 meses después. Genera cada razón genuina por la que podría haber muerto. Sé exhaustivo. Sé específico. Fundamenta cada razón en los detalles reales del plan. No rellenes con razones débiles y no pares antes de tiempo si hay más."

El resultado debe ser una lista completa de razones de fallo, cada una expresada en 1-2 frases. Sé honesto y exhaustivo. Algunos planes pueden tener 4 modos de fallo genuinos. Otros pueden tener 9. El número debe ser el que sea real para este plan específico.

Cada razón de fallo debe ser:
- Específica de este plan (no un consejo genérico que aplique a cualquier cosa)
- Fundamentada en detalles reales que el usuario proporcionó
- Una amenaza genuina (no un inconveniente menor o un caso extremadamente improbable)

### paso 3: agentes de análisis profundo

Toma cada razón de fallo del paso 2 y analízala en profundidad de forma independiente. Cada análisis debe tratarse como si fuera ejecutado por un investigador diferente: sin dejar que el análisis de una razón influya en el de las siguientes.

**Nota sobre paralelismo:** si el entorno soporta ejecución paralela real (Claude con herramientas de agentes), lanza un sub-agente por razón de fallo de forma simultánea. Si el entorno ejecuta de forma secuencial (como claude.ai estándar), procesa cada razón de forma secuencial pero con el mismo aislamiento: analiza cada una como si no hubieras analizado las anteriores, sin dejar que los patrones emergentes de los análisis previos contaminen el siguiente.

**Plantilla de análisis por razón de fallo:**

```
Eres un investigador en un análisis de premortem. Se te ha asignado una razón de fallo específica para analizar en profundidad.

El plan:
---
[contexto completo: qué es, para quién es, cómo es el éxito, más contexto relevante del espacio de trabajo]
---

ENCUADRE DEL PREMORTEM: Han pasado 6 meses. Este plan ha fallado.

TU RAZÓN DE FALLO ASIGNADA: [la razón de fallo específica del paso 2]

Tu trabajo es profundizar en este fallo. Escribe la historia de cómo se desarrolló realmente. Sé específico. Usa detalles del plan. Hazlo sentir real, como un estudio de caso de algo que realmente ocurrió.

Tu resultado debe incluir:

1. LA HISTORIA DEL FALLO: Una narrativa de 2-3 párrafos de cómo se desarrolló este fallo específico. Usa detalles del plan. Nombra momentos específicos donde las cosas salieron mal y por qué.

2. EL SUPUESTO SUBYACENTE: La única cosa que el usuario daba por sentada y que hizo posible este fallo. Exprésalo en una frase.

3. SEÑALES DE ADVERTENCIA TEMPRANAS: 1-2 señales concretas y observables que el usuario podría vigilar y que indicarían que este modo de fallo está empezando a desarrollarse. Deben ser cosas que se puedan ver o medir realmente, no sensaciones vagas.

4. EVALUACIÓN DE RIESGO:
   - Probabilidad: Alta / Media / Baja (con una frase de justificación)
   - Severidad: Alta / Media / Baja (con una frase de justificación)

Sé conciso. La mayoría de análisis cabrán en 200 palabras; ninguno debería superar 400. No lo atenúes. No lo suavices.
```

El campo de evaluación de riesgo (probabilidad + severidad) es nuevo respecto a la versión original: tenerlo estructurado desde el análisis hace que la síntesis sea más consistente y permite comparar modos de fallo entre sí de forma objetiva.

### paso 4: síntesis

Después de completar todos los análisis en profundidad, produce la síntesis:

**INFORME DE PREMORTEM**

1. **El Fallo Más Probable** — ¿Qué escenario de fallo tiene la probabilidad más alta según los análisis? ¿Por qué? Este es en el que el usuario debe enfocarse primero.

2. **El Fallo Más Peligroso** — ¿Qué escenario de fallo tiene la severidad más alta aunque sea menos probable? Este es el que vale la pena asegurar aunque cueste esfuerzo.

3. **El Supuesto Oculto** — De todos los análisis de fallo, ¿cuál es el supuesto más importante que el usuario está haciendo y que probablemente no ha cuestionado? Aquí es donde a menudo vive el valor real del premortem: lo que es tan obvio para el usuario que olvidó que era un supuesto.

4. **El Plan Revisado** — Basándose en los escenarios de fallo, ¿qué cambios específicos harían el plan más resiliente? Sé concreto. No digas "considera tu precio". Di "prueba el precio en $X con 20 personas antes de comprometerte públicamente". Cada revisión debe corresponderse directamente con un escenario de fallo específico.

5. **La Lista de Verificación Pre-Lanzamiento** — 3-5 cosas específicas que el usuario debe verificar, probar o implementar antes de ejecutar. Cada una debe prevenir o detectar uno de los modos de fallo identificados.

### paso 5: generar el informe visual

**Detección del entorno antes de generar el informe:**

- Si tienes acceso a filesystem (Claude con computer use o entorno de agentes), genera un archivo HTML autocontenido y guárdalo como `premortem-report-[timestamp].html`. Ábrelo después de generarlo.
- Si no tienes acceso a filesystem (claude.ai estándar sin computer use), presenta el informe directamente en el chat usando Markdown estructurado con las mismas secciones. No intentes crear archivos que no puedes guardar.

**Estructura del informe HTML (cuando aplique):**

El informe debe ser un único archivo HTML autocontenido con CSS en línea. Principios de diseño:
- Fondo oscuro (#0a0e1a o similar), tipografía limpia, fácil de escanear
- La sección de síntesis (fallo más probable, fallo más peligroso, supuesto oculto, plan revisado, lista de verificación) debe mostrarse de forma prominente al principio
- Una tarjeta visual por razón de fallo que muestre: la razón como encabezado, la historia del fallo, el supuesto subyacente, las señales de advertencia tempranas, y los indicadores de probabilidad/severidad como badges visuales (Alto = rojo, Medio = naranja, Bajo = amarillo)
- Usa colores de acento distintos por tarjeta para que sean visualmente escaneables
- Pie de página con marca de tiempo y qué fue sometido al premortem

**Estructura del informe Markdown (cuando no hay filesystem):**

Usa los mismos encabezados y secciones, con tablas o listas para los indicadores de riesgo. La legibilidad importa más que la estética en este modo.

### paso 6: guardar la transcripción (solo con filesystem)

Si tienes acceso a filesystem, guarda la transcripción completa como `premortem-transcript-[timestamp].md`. Incluye:
- El contexto que se recopiló (qué, quién, criterios de éxito, contexto enriquecido si lo hubo)
- Las razones de fallo del premortem en bruto
- Todos los análisis en profundidad
- La síntesis completa

Si no tienes acceso a filesystem, omite este paso. El informe en el chat ya contiene todo.

### paso 7: validación post-síntesis

Después de presentar el informe (en cualquier formato), cierra con esta pregunta al usuario:

"¿Alguno de estos modos de fallo ya lo habías considerado? ¿Hay contexto que no mencionaste antes y que cambiaría el análisis?"

Esta pregunta tiene dos propósitos: captura información que el usuario no verbalizó en el contexto inicial, y convierte el premortem en un diálogo en lugar de un monólogo. Si el usuario aporta nuevo contexto relevante, ajusta la síntesis y el plan revisado en consecuencia.

---

## formato de salida

**Con acceso a filesystem:**
```
premortem-report-[timestamp].html    # informe visual para escanear
premortem-transcript-[timestamp].md  # transcripción completa como referencia
```
El usuario ve primero el informe HTML. La transcripción está disponible si quiere profundizar en el razonamiento.

**Sin acceso a filesystem:**
El informe completo se presenta directamente en el chat con formato Markdown. Misma estructura, mismas secciones, sin pérdida de contenido.

En ambos casos, proporciona también un resumen conciso en el chat: el fallo más probable, el supuesto oculto y la única revisión más importante del plan. Máximo tres frases.

---

## ejemplos

### ejemplo 1: lanzamiento de producto (B2C)

**Usuario:** "premortem esto: estoy a punto de lanzar un taller en vivo de $297 sobre cómo usar Claude Cowork para equipos de marketing. 50 plazas. Dirigido a directores de marketing en empresas con 10-50 empleados."

**El premortem en bruto identifica 6 razones de fallo:**
1. Los directores de marketing en empresas de este tamaño necesitan aprobación para gastar $297 en desarrollo profesional, añadiendo fricción que no has tenido en cuenta
2. "Claude Cowork para marketing" es un pitch centrado en una herramienta en un mercado donde la mayoría de los directores todavía están decidiendo si la IA es relevante para ellos
3. La audiencia que realmente compra podría ser solopreneurs, no directores de equipo, creando un desajuste entre el contenido y los asistentes
4. Construir un taller para equipos de marketing requiere entornos de demostración con datos de marketing realistas y configuraciones multiusuario, lo que lleva 5 semanas de preparación, no las 2 que has presupuestado
5. Si el 60% de los asistentes son solopreneurs, tus reseñas y casos de estudio no resonarán con la audiencia de directores de marketing que necesitas para cohortes futuras
6. A $297 con 50 plazas, el ingreso máximo es $14.850, lo que puede no justificar el tiempo de preparación frente a otras oportunidades de ingresos

Cada razón se analiza en profundidad de forma independiente. Ejemplo de evaluación de riesgo para la razón 3:
- Probabilidad: **Alta** — el precio y el canal de marketing (LinkedIn, newsletters) atraen naturalmente a solopreneurs que investigan herramientas por su cuenta.
- Severidad: **Alta** — el desajuste de audiencia corrompe el activo más valioso del taller: los testimonios y casos de estudio para cohortes futuras.

**Síntesis:** El fallo más probable es el desajuste de audiencia: estás apuntando a personas que necesitan aprobación para gastar $297, lo que añade fricción que no has tenido en cuenta. El fallo más peligroso: atraer solopreneurs en lugar de directores de equipo significa que tus casos de estudio y testimonios no resonarán con el comprador objetivo real para cohortes futuras, agravando el problema con el tiempo. Supuesto oculto: asumes que "directores de marketing en empresas de 10-50 personas" es una audiencia alcanzable, pero estas personas no se identifican de esa manera y no están en los mismos lugares. Plan revisado: ejecuta una sesión piloto de $47 para 20 personas primero. Usa eso para identificar si tus compradores reales son directores de equipo o solopreneurs, y construye el taller completo para quien realmente aparezca.

---

### ejemplo 2: contratación (decisión de equipo)

**Usuario:** "premortem esto: voy a contratar a un desarrollador senior remoto full-time para liderar el frontend de mi startup. Tengo un candidato fuerte, quiero cerrar esta semana."

**El premortem en bruto identifica 5 razones de fallo:**
1. La urgencia de cerrar esta semana comprime el proceso de referencia: no verificarás cómo trabaja bajo presión o en conflictos de criterio técnico
2. "Liderar el frontend" implica influencia sobre decisiones de arquitectura, pero no has validado si el candidato toma decisiones o ejecuta decisiones de otros
3. El trabajo remoto full-time requiere autodirección; una entrevista no revela si el candidato funciona bien sin supervisión diaria
4. No conoces sus expectativas reales de crecimiento: si espera pasar a tech lead en 12 meses y no hay ese camino, se irá
5. El stack actual puede cambiar en 6 meses; si el candidato es experto en tecnología X y el roadmap se mueve a Y, su ventaja competitiva desaparece

Evaluación de riesgo para la razón 1:
- Probabilidad: **Alta** — cerrar en una semana es una señal de presión que casi siempre acorta la debida diligencia.
- Severidad: **Alta** — una mala contratación a nivel senior cuesta entre 6 y 18 meses de recuperación, más el costo de reemplazar.

**Síntesis:** El fallo más probable es contratar a alguien que ejecuta bien pero no lidera, descubriendo el desajuste cuando ya es tarde para corregirlo sin fricción. El supuesto oculto: asumes que "desarrollador senior fuerte" y "alguien que puede liderar un frontend" son la misma cosa. Plan revisado: antes de cerrar, añade una sesión de 90 minutos donde el candidato tome una decisión de arquitectura real con restricciones reales y la defienda ante objeciones. Eso revela más que cinco entrevistas estándar.

---

## notas importantes

- **Mantén el aislamiento entre análisis.** Ya sea paralelo o secuencial, cada razón de fallo debe analizarse sin contaminación de las otras. El paralelismo es una optimización de velocidad, no el mecanismo de calidad.
- **Siempre establece el encuadre del premortem explícitamente.** "Esto ya ha fallado" es el mecanismo psicológico que hace que esto funcione. Sin él, el análisis vuelve a ser una evaluación de riesgos cortés en lugar de una identificación honesta de fallos.
- **Sé exhaustivo pero no rellenes.** Encuentra cada razón de fallo genuina. No pares en 3 si hay 7. Pero no fuerces 7 si solo hay 3. El número debe ser el que sea real para este plan específico.
- **La síntesis es el producto.** La mayoría de los usuarios leerán la síntesis y hojearán las tarjetas de fallo individuales. Haz la síntesis específica y accionable.
- **No suavices.** El objetivo de un premortem es decirle al usuario cosas que no quiere escuchar antes de que lo haga la realidad. Si un plan tiene problemas serios, dilo directamente.
- **El plan revisado debe ser concreto.** No digas "considera probar tu precio". Di "ejecuta un piloto de $47 con 20 personas antes de comprometerte con el taller completo de $297". Cada revisión debe ser algo que el usuario pueda hacer realmente esta semana.
- **Respeta el umbral mínimo de contexto.** Ejecutar un premortem con contexto insuficiente produce fallos genéricos que desperdician el tiempo del usuario. Es mejor hacer una pregunta más que producir un mal premortem.
- **Detecta el entorno antes de generar archivos.** Intentar crear archivos en un entorno sin filesystem produce errores silenciosos o confusión. Adapta el formato de salida al entorno real, no al ideal.
- **Esto no es el Consejo LLM.** El consejo da múltiples perspectivas sobre una decisión ahora mismo. El premortem envía a Claude al futuro donde la decisión ya falló y trabaja hacia atrás para explicar por qué. Mecanismo psicológico diferente, resultado diferente. Si el usuario parece querer múltiples perspectivas en lugar de un análisis de fallos, sugiere el consejo en su lugar.
