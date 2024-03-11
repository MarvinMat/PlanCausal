[Zurück zur Gliederung](../../readme.md)

<br />

# Auswahl eines geeigneten Simulationsframeworks als Grundlage für die Simulationsbibliothek

- [Kriterien für den Vergleich verschiedener Simulationsframeworks](#kriterien-für-den-vergleich-verschiedener-simulationsframeworks)
- [Vergleich verschiedener Simulationsframeworks](#vergleich-verschiedener-simulationsframeworks)
- [Auswahl eines geeigneten Simulationsframeworks](#auswahl-eines-geeigneten-simulationsframeworks)
- [Anmerkung](#anmerkung)

## Kriterien für den Vergleich verschiedener Simulationsframeworks

Im Rahmen des sorgfältigen Auswahlprozesses von Simulationsframeworks für das Master-Forschungsprojekt wurden diverse Kriterien herangezogen, um eine fundierte Entscheidung zu treffen, die sowohl den wissenschaftlichen Ansprüchen gerecht wird als auch die praktische Durchführbarkeit berücksichtigt. Ein zentrales Augenmerk lag auf dem funktionalen Umfang der Frameworks, um eine adäquate Abbildung und Analyse der komplexen Systemdynamiken zu gewährleisten. Die Performanz der betrachteten Tools wurde ebenso eingehend bewertet, da eine hohe Verarbeitungsgeschwindigkeit für die effiziente Simulation anspruchsvoller Modelle unerlässlich ist.

Die Benutzerfreundlichkeit stellte ein weiteres entscheidendes Kriterium dar; hierbei wurde insbesondere auf intuitive Bedienbarkeit und die Verfügbarkeit von Dokumentation und Lernressourcen Wert gelegt, um eine steile Lernkurve für das Forschungsteam zu vermeiden. Die Kostenstruktur der Frameworks wurde ebenfalls berücksichtigt, wobei eine Präferenz für lizenzkostenfreie oder Open-Source-Optionen bestand, um die Zugänglichkeit des Projekts zu maximieren und gleichzeitig die Forschungskosten zu minimieren.

Die Größe und Aktivität der Entwickler- und Anwendergemeinschaft jedes Frameworks sowie die Qualität des verfügbaren Supports wurden als Indikatoren für die Langzeitviabilität und die Unterstützung bei der Problemlösung herangezogen. Nicht zuletzt spielte die Kompatibilität der Programmiersprachen eine wesentliche Rolle, um eine nahtlose Integration in bestehende Forschungsinfrastrukturen und die Nutzung vorhandener Kenntnisse innerhalb des Teams zu gewährleisten.

## Vergleich verschiedener Simulationsframeworks

<style>
#table1 table tbody tr:nth-child(1),
#table1 table tbody tr:nth-child(11) {
    background-color: #009787;
    color: black;
}

#table1 table tbody tr:nth-child(11) {
    font-weight: bold;
}
</style>

<div id="table1">

| Framework       | Umfang | Performance | Anwenderfreundlichkeit | Preis                         | Community/Support | Programmiersprache    |
| --------------- | ------ | ----------- | ---------------------- | ----------------------------- | ----------------- | --------------------- |
| SimPy           | Mittel | Mittel      | Hoch                   | Kostenlos                     | Mittel            | Python                |
| AnyLogic        | Hoch   | Hoch        | Mittel                 | Kostenlos (PLE) / Kommerziell | Hoch              | Java                  |
| OMNeT++         | Hoch   | Hoch        | Mittel                 | Kostenlos (GPL)               | Hoch              | C++                   |
| Arena           | Hoch   | Hoch        | Hoch                   | Kommerziell                   | Hoch              | Windows (GUI)         |
| DEVS            | Hoch   | Mittel      | Mittel                 | Kostenlos                     | Mittel            | Mehrere (C++, Python) |
| ns-3            | Hoch   | Hoch        | Mittel                 | Kostenlos                     | Hoch              | C++/Python            |
| MATLAB/Simulink | Hoch   | Hoch        | Hoch                   | Kommerziell                   | Hoch              | MATLAB                |
| ExtendSim       | Hoch   | Hoch        | Hoch                   | Kommerziell                   | Mittel            | Windows (GUI)         |
| DESMO-C#        | Mittel | Mittel      | Mittel                 | Kostenlos                     | Gering            | C# (.NET)             |
| Sim.NET         | Mittel | Mittel      | Mittel                 | Kostenlos                     | Gering            | C# (.NET)             |
| Sim#            | Mittel | Hoch        | Hoch                   | Kostenlos (MIT)               | Gering            | C# (.NET)             |

</div>

## Auswahl eines geeigneten Simulationsframeworks

Nach eingehender Analyse und Abwägung dieser Kriterien fiel die Entscheidung auf die Frameworks SimPy und Sim#, die nicht nur durch ihren robusten Funktionsumfang und ihre Performanz überzeugen, sondern auch durch ihre ausgezeichnete Benutzerfreundlichkeit, ihre kostenfreie Verfügbarkeit sowie ihre aktive Community und Supportstrukturen. Zudem bieten sie die Flexibilität der Nutzung in den für das Projekt relevanten Programmiersprachen, was eine effiziente und effektive Durchführung der Forschungsarbeit ermöglicht.

## Anmerkung

Das Team entschied sich für eine typisierte Programmiersprache wie C#, da vorauszusehen war, dass der Umfang und die Komplexität der zu entwickelnden Software deutlich über ein kleines Projekt hinausgehen würden. Eine Sprache wie Python schien, auch aufgrund mangelnder Erfahrung und Verständnis innerhalb des Teams, nicht geeignet für ein Projekt dieser Größenordnung. Die notwendige Einarbeitungszeit, die Pflege der Bibliotheken und die Akkumulation des erforderlichen Wissens hätten den Erfolg des Projektes potenziell gefährden können.

Nach einer sorgfältigen Evaluation der beiden Frameworks SimPy und SimSharp stellte sich heraus, dass SimSharp, als Implementierung des populären SimPy im .NET-Ökosystem, eine umfangreiche Palette an Komfortfunktionen und Methoden bietet. Diese Funktionen und Methoden hätten in Python bzw. SimPy erst mit erheblichem Aufwand selbst implementiert werden müssen. SimSharp erweist sich daher als die überlegene Wahl, indem es eine effizientere Entwicklungsumgebung für unser Projekt bereitstellt und somit die Realisierung komplexer Simulationsszenarien erleichtert.

Genauere Erläuterungen sind im [Architekturteil](architecture.md) dieses Berichtes zu finden.

<br /><br />

[Zurück zur Gliederung](../../readme.md)
