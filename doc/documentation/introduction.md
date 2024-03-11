[Zurück zur Gliederung](../../readme.md)

<br />

# Einführung

- [Ziel](#ziel)
- [Anforderungen](#anforderungen)
- [Kurzbeschreibung](#kurzbeschreibung)

## Ziel

Ziel dieser Anwendung ist es, dem Anwender die Möglichkeit zu geben, eine Produktion möglichst realistisch zu simulieren.
Dabei soll der Anwender die Möglichkeit haben, die Produktion zu konfigurieren und diese anschließend simulieren zu lassen.
Es sollen dabei außerdem zufällige Ereignisse und Unterbrechungen simuliert werden, so wie sie auch in der Realität auftreten würden.

Langfristig soll die Anwendung die Grundlage für ein Machine Learning Projekt sein, welches die Planung von Produktionsprozessen optimiert.
Aus diesem Grund bietet die Anwendung nach Abschluss der Simulation verschiedene Kennzahlen und Statistiken an, anhand derer der Erfolg der
Produktion gemessen und beurteilt werden kann.

Darüber hinaus ist die Anwendung in der Lage, gesteuert durch ein kausales Modell, Daten für die Simulation zu erzeugen, die basierend auf diesem Modell die Durchlaufzeiten der jeweiligen Arbeitsgänge der simulierten Produktion verändern.

Das hinterlegte Modell soll mit Hilfe von maschinellen Lernverfahren anschließend aus den generierten bzw. simulierten Daten, die in Form von Rückmeldungen vorliegen, erneut erlernt werden können.

Das primäre Ziel dieses Forschungsprojektes besteht in der Optimierung realer Produktionsprozesse mittels intelligenter Planungs- und Steueralgorithmen. Diese Algorithmen sollen fähig sein, auf Basis von historischen Datenmustern oder durch Simulation gewonnenen Erkenntnissen, zukünftige Planungsprozesse derart zu optimieren, dass die Notwendigkeit von Neuplanungen sowie Abweichungen vom vorgesehenen Plan minimiert werden. Das Forschungsteam erhofft sich, durch diese Optimierung signifikante Verbesserungen in Bezug auf Kosten- und Zeitmanagement zu erzielen.

## Anforderungen

- Die Anwendung soll in Python nutzbar sein.
- Es soll möglich sein, einen vorgegebenen Produktionsplan zu simulieren.
- Es soll möglich sein, einen Steuerungsalgorithmus vorzugeben, der auf Ereignisse, die während der Simulation auftreten, reagiert und
  entsprechende Anpassungen an dem aktuellen Produktionsplan vornimmt.
- Es soll möglich sein, einen Planungsalgorithmus vorzugeben, der die zu simulierenden Vorgänge plant und so die Simulation dieses Plans ermöglicht.
- Die Anwendung soll die Möglichkeit bieten, einen Produktionsprozess zu konfigurieren. Dabei sollen konfigurierbar sein:
  - Stammdaten der Produktion:
    - Arbeitspläne
    - Maschinen
    - Werkzeuge
  - Zufällige Ereignisse, die beispielsweise für eine Unterbrechung einzelner Vorgänge sorgen
  - Konkrete Produktionsaufträge, die simuliert werden sollen
- Die Anwendung soll die Ergebnisse der Simulation (in Form von verschiedenen Kennzahlen) zur Verfügung stellen.
- Die Anwendung soll die Möglichkeit bieten, die Ergebnisse der Simulation zu visualisieren.
- Die Anwendung soll dem Nutzer die Möglichkeit geben, mit Hilfe eines bestimmten Kausalen Modells, Daten nach diesem Modell zu erzeugen.
- Die Anwendung soll mit Hilfe von Einflussfaktoren die Dauer der simulierten Arbeitsgänge verlängern oder verkürzen.
- Die Anwendung soll das hinterlegte Kausale Modell durch maschinelles Lernen wieder erzeugen können.

## Kurzbeschreibung

Die Anwendung besteht aus drei Hauptkomponenten, die im Folgenden kurz beschrieben werden.

Die erste Komponente ist der **Planer**. Dieser ist für die Planung der zu simulierenden Arbeitsgänge zuständig. Er erhält die zu planenden Arbeitsgänge und die Maschinen, auf denen diese ausgeführt werden sollen, und plant die Reihenfolge der Arbeitsgänge. Dabei weist er ihnen Start- und Endzeitpunkt zu. Um eigene Planer-Implementierungen zu ermöglichen, können dieser von der abstrakten `Planner`-Klasse abgeleitet werden. Eine Implementierung eines Planers, der auf dem Giffler-Thompson-Algorithmus basiert, ist bereits vorhanden (siehe [GifflerThompsonPlanner.cs](Planner.Implementation/GifflerThompsonPlanner.cs)).

Die zweite Komponente ist der **Simulator**. Dieser erhält die vom Planer geplanten Arbeitsgänge und simuliert diese. Dabei werden die Arbeitsgänge auf den Maschinen ausgeführt und die Rüstzeiten zwischen den Arbeitsgängen berücksichtigt. Außerdem werden zufällige Ereignisse simuliert, die beispielsweise für eine Unterbrechung einzelner Arbeitsgänge sorgen.

Die dritte Komponente ist die **Steuerung**. Sie ist die Verbindung zwischen den beiden anderen Komponenten. Die Steuerung erhält die vom Simulator geworfenen Ereignisse und reagiert auf diese. Dabei kann sie beispielsweise die Reihenfolge der Arbeitsgänge ändern oder einzelne Arbeitsgänge auf andere Maschinen verschieben. Regelmäßig wird von der Simulation ein Neuplanungs-Event ausgelöst, welches von der Steuerung behandelt wird. Dabei kann sie den Planer aufrufen und die Planung der Arbeitsgänge neu anstoßen. Der Planer plant dann die Arbeitsgänge neu, der neue Plan wird von der Steuerung an den Simulator übergeben und die Simulation wird fortgesetzt.

Nähere Beschreibungen befinden sich in den folgenden Kapiteln.

Ein ausführliches Beispiel, wie diese Software zu verwenden ist, inklusive detaillierter Erklärungen folgt weiter unten (siehe [Beispiel Anwendung in .NET](application.md#beispiel-anwendung-in-net) bzw. [Beispiel Anwendung in Python](application.md#beispiel-anwendung-in-python)).

<br /><br />

[Zurück zur Gliederung](../../readme.md)
