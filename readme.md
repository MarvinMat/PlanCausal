# Forschungs- und Entwicklungsprojekt "REPLAKI"

- [Einführung](#einfuhrung)
  * [Ziel](#ziel)
  * [Anforderungen](#anforderungen)
  * [Kurzbeschreibung](#kurzbeschreibung)
- [Konfiguration](#konfiguration)
  * [Maschinen](#maschinen)
  * [Werkzeuge](#werkzeuge)
  * [Arbeitspläne](#arbeitsplane)
- [Beispiel Anwendung in .NET](#beispiel-anwendung-in-net)
- [Beispiel Anwendung in Python](#beispiel-anwendung-in-python)
  * [Unterschiede zwischen Python und .NET](#unterschiede-python-net)
  * [Ausführen der Anwendung](#ausfuhren-der-anwendung)

## Einführung

### Ziel
Ziel dieser Anwendung ist es, dem Anwender die Möglichkeit zu geben, eine Produktion möglichst realistisch zu simulieren.
Dabei soll der Anwender die Möglichkeit haben, die Produktion zu konfigurieren und diese anschließend simulieren zu lassen.
Es sollen dabei außerdem zufällige Ereignisse und Unterbrechungen simuliert werden, so wie sie auch in der Realität auftreten würden.

Langfristig soll die Anwendung die Grundlage für ein Machine Learning Projekt sein, welches die Planung von Produktionsprozessen optimiert.
Aus diesem Grund bietet die Anwendung nach Abschluss der Simulation verschiedene Kennzahlen und Statistiken an, anhand derer der Erfolg der
Produktion gemessen und beurteilt werden kann.


### Anforderungen

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

### Kurzbeschreibung

Die Anwendung besteht aus drei Hauptkomponenten, die im Folgenden kurz beschrieben werden.

Die erste Komponente ist der **Planer**. Dieser ist für die Planung der zu simulierenden Arbeitsgänge zuständig. Er erhält die zu planenden Arbeitsgänge und die Maschinen, auf denen diese ausgeführt werden sollen, und plant die Reihenfolge der Arbeitsgänge. Dabei weist er ihnen Start- und Endzeitpunkt zu. Um eigene Planer-Implementierungen zu ermöglichen, können dieser von der abstrakten ```Planner```-Klasse abgeleitet werden. Eine Implementierung eines Planers, der auf dem Giffler-Thompson-Algorithmus basiert, ist bereits vorhanden (siehe [GifflerThompsonPlanner.cs](Planner.Implementation/GifflerThompsonPlanner.cs)).

Die zweite Komponente ist der **Simulator**. Dieser erhält die vom Planer geplanten Arbeitsgänge und simuliert diese. Dabei werden die Arbeitsgänge auf den Maschinen ausgeführt und die Rüstzeiten zwischen den Arbeitsgängen berücksichtigt. Außerdem werden zufällige Ereignisse simuliert, die beispielsweise für eine Unterbrechung einzelner Arbeitsgänge sorgen.

Die dritte Komponente ist die **Steuerung**. Sie ist die Verbindung zwischen den beiden anderen Komponenten. Die Steuerung erhält die vom Simulator geworfenen Ereignisse und reagiert auf diese. Dabei kann sie beispielsweise die Reihenfolge der Arbeitsgänge ändern oder einzelne Arbeitsgänge auf andere Maschinen verschieben. Regelmäßig wird von der Simulation ein Neuplanungs-Event ausgelöst, welches von der Steuerung behandelt wird. Dabei kann sie den Planer aufrufen und die Planung der Arbeitsgänge neu anstoßen. Der Planer plant dann die Arbeitsgänge neu, der neue Plan wird von der Steuerung an den Simulator übergeben und die Simulation wird fortgesetzt.

![image](doc/Diagramme/Sequenzdiagramm.svg)

Ein ausführliches Beispiel, wie diese Software zu verwenden ist, inklusive detaillierter Erklärungen folgt weiter unten (siehe [Beispiel Anwendung in .NET](#beispiel-anwendung-in-net) bzw. [Beispiel Anwendung in Python](#beispiel-anwendung-in-python)).

## Konfiguration
Die Konfigurierbarkeit der Applikation ist ein zentrales Features und ist für die Komplexität des zugrundeliegenden Problems von großer Bedeutung. Die Konfiguration der Stammdaten erfolgt über JSON-Dateien, die die verschiedenen Ressourcen und Parameter enthalten. Die Konfigurationen werden in den folgenden Abschnitten genauer beschrieben.

### Maschinen
Die in der Produktion vorhandenen Maschinen werden anhand ihres entsprechenden Typs konfiguriert. Dabei wird vereinfacht angenommen, dass jede Maschine eines Typs die gleichen Eigenschaften besitzt. Für jeden Maschinentyp sind folgende Eigenschaften konfigurierbar:
- ```typeId```: eine eindeutige ID
- ```count```: die Anzahl der vorhandenen Maschinen dieses Typs
- ```name```: ein Name
- ```allowedToolIds```: eine Liste von auf diesem Maschinentyp erlaubten Werkzeugen (dabei werden die Typ-Ids der jeweiligen Werkzeuge angegeben, siehe [Werkzeuge](#werkzeuge))
- ```changeoverTimes```: eine Rüstzeitmatrix. Diese ist wie folgt aufgebaut: Das Element in Zeile x und Spalte y enthält die Rüstzeit in Minuten, die benötigt wird, um auf diesem Maschinentyp von dem Werkzeug an der x-ten Stelle im ```allowedToolIds```-Array umzurüsten auf das Werkzeug an der y-ten Stelle in dem Array. In den Diagonalelementen (Zeile z, Spalte z) der Matrix können somit auch Rüst- oder Vorbereitungszeiten angegeben werden, die bei der Nutzung eines bestimmten Werkzeugs vor jedem Arbeitsgang anfallen.
Ein Beispiel ist [hier](#rustzeitbsp) genauer beschrieben.

Die Maschinen werden in der Datei [machines.json](Machines.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel für die Konfiguration einer Maschine.

```json
{
    "typeId": 1,
    "count": 1,
    "name": "Machine 1",
    "allowedToolIds" : [1,3,4],
    "changeoverTimes": [
        [0.0, 5.0, 12.5],
        [5.0, 0.0, 10.0],
        [12.5, 10.0, 2.0]]
}
```

<a id="rustzeitbsp"></a>
Die auf dieser Maschine erlaubten Werkzeuge sind die Werkzeuge 1, 3 und 4.

Die Rüstzeit-Matrix enthält unter anderem folgende Angaben: In der ersten Zeile und zweiten Spalte sind 5 Minuten Rüstzeit angegeben.
Das bedeutet, dass der Wechsel vom ersten Werkzeug im ```allowedToolIds```-Array (Werkzeug 1) zum zweiten Werkzeug in diesem Array (Werkzeug 3) durchschnittlich 5 Minuten dauert.
In Zeile 2, Spalte 3 ist angegeben, dass der Wechsel von Werkzeug 3 (2-tes Werkzeug im ```allowedToolIds```-Array) zu Werkzeug 4 (3-tes Werkzeug im ```allowedToolIds```-Array) 10 Minuten dauert. Außerdem ist im dritten Diagonalelement (Zeile 3, Spalte 3) angegeben, dass vor jedem Arbeitsgang mit dem dritten Werkzeug (Werkzeug 4) 2 Minuten Rüst- bzw. Vorbereitungszeit benötigt werden.

In diesem Beispiel ist die Matrix symmetrisch, dies ist aber nicht notwendigerweise der Fall.

### Werkzeuge
Werkzeuge werden ebenfalls anhand ihres Typs konfiguriert. Ein Werkzeugtyp kann dabei ein bestimmtes tatsächlich existierendes Werkzeug beschreiben, aber kann auch einen bestimmten Operationsmodus einer Maschine abbilden. 
Für ein Werkzeug sind folgende Eigenschaften konfigurierbar:
- ```typeId```: eine eindeutige ID
- ```name```: ein Name
- ```description```: eine Beschreibung.

Die Werkzeuge werden in der Datei [tools.json](Tools.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel für die Konfiguration eines Werkzeugs.

```json
{
    "typeId": 1,
    "name": "Tool 1",
    "description": "Tool 1"
}
```

### Arbeitspläne
Arbeitspläne sind die dem Produktionsprozess zugrunde liegenden Stammdaten. Sie beschreiben, welche Arbeitsschritte notwendig sind, um ein bestimmtes Produkt herzustellen. 
Die Arbeitspläne werden dabei als eine Liste von Arbeitsgängen (alternativ: Arbeitsplanpositionen) beschrieben. Für jeden Arbeitsplan sind die folgenden Eigenschaften konfigurierbar:
- ```workPlanId```: eine eindeutige Id
- ```name```: der Name des Produkts
- ```operations```: ein Array von zugehörigen Arbeitsgängen. Jeder einzelne Arbeitsgang ist dabei wieder ein Objekt, für welches folgende Eigenschaften konfigurierbar sind:
    - ```name```: der Name
    - ```duration```: die Bearbeitungszeit (ohne Rüsten) in Minuten
    - ```machineId```: die ID des Maschinentyps, auf dem dieser Arbeitsgang ausgeführt werden soll
    - ```toolId```: die ID des zu verwendenden Werkzeugtyps.
    
Die Arbeitspläne werden in der Datei [workplans.json](WorkPlans.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel für die Konfiguration eines Arbeitsplans.

```json
{
    "workPlanId": 1,
    "name": "Tisch"
    "operations": [
        {
            "machineId": 1,
            "duration": 15,
            "name": "Tischbein sägen",
            "toolId": 2
        },
        {
            "machineId": 2,
            "duration": 10,
            "name": "Tischbein schleifen",
            "toolId": 1
        },
        {
            "machineId": 3,
            "duration": 5,
            "name": "Tischbein lackieren",
            "toolId": 3
        }
    ]
},
```


## Beispiel Anwendung in .NET <a id="beispiel-anwendung-in-net"></a>
Der in der Datei [Main.cs](ProcessSimulator/Main.cs) vorliegende Code implementiert eine beispielhafte Anwendung der Simulation zur Produktionsplanung und -steuerung. Im Folgenden wird der Aufbau und die Funktionsweise beschrieben:

Zunächst werden die benötigten Ressourcen wie Maschinen, Werkzeuge und Arbeitspläne aus JSON-Dateien geladen. Diese Ressourcen dienen als Grundlage für die Simulation und Planung der Produktion.

```csharp
IMachineProvider machineProvider = new MachineProviderJson("../../../../Machines.json");
var machines = machineProvider.Load();

IToolProvider toolProvider = new ToolProviderJson("../../../../Tools.json");
var tools = toolProvider.Load();

IWorkPlanProvider workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
var plans = workPlanProvider.Load();
```

Nachdem die Ressourcen geladen wurden, wird für jeden Arbeitsplan (also jedes Produkt) ein Produktionsauftrag erstellt, der simuliert werden soll. Dabei wird angegeben wie viele Einheiten des Produkts hergestellt werden sollen (```Quantity```).

```csharp
var orders = plans.Select(plan => new ProductionOrder()
{
    Name = $"Order {plan.Name}",
    Quantity = 60,
    WorkPlan = plan,
}).ToList();
```

Anschließend werden aus den Aufträgen die entsprechenden ```WorkOperation```-Objekte erstellt. Diese Objekte enthalten Informationen über die einzelnen Arbeitsgänge und deren Abhängigkeiten.
Die Methode ```ModelUtil.GetWorkOperationsFromOrders``` übernimmt das Erstellen der einzelnen ```WorkOperation```-Objekte und verknüpft diese untereinander.
Jedes ```WorkOperation```-Objekt kennt seinen Vorgänger und seinen Nachfolger, falls diese existieren.

```csharp
var operations = ModelUtil.GetWorkOperationsFromOrders(orders);
```

Als nächstes wird ein Simulator erstellt, der einen Seed für die Zufallszahlengenerierung erhält, sowie ein Startdatum der Simulation. Diese Klasse übernimmt das Simulieren der Produktion.

```csharp
var simulator = new Simulator(rnd.Next(), DateTime.Now);
```

Für die Simulation der Produktion wird im nächsten Schritt eine zufällige Unterbrechung hinzugefügt. Beispielhaft wird hier ein Stromausfall modelliert, der Vorgänge auf allen Maschinen unterbricht.
Die Methode ```AddInterrupt``` erhält als ersten Parameter eine Funktion, die bestimmt, ob ein Vorgang unterbrochen werden soll. Sie wird für jeden Prozess in der Simulation aufgerufen und gibt einen Wahrheitswert zurück, der bestimmt, ob dieser Prozess unterbrochen werden soll. In diesem Beispiel wird jeder Vorgang unterbrochen, möglich wäre es aber beispielsweise auch, nur Vorgänge auf einer bestimmten Maschine oder einem bestimmten Maschinentyp zu unterbrechen.
Als zweiten Parameter erhält die Methode eine Verteilung, die bestimmt, wie lange die Unterbrechung dauert. In diesem Beispiel wird eine exponentialverteilte Zufallsvariable mit einer mittleren Dauer von 5 Stunden verwendet.
Als dritten Parameter erhält die Methode eine Funktion, die ausgeführt wird, wenn ein Vorgang unterbrochen wird. Dabei kann beispielsweise die Unterbrechung behandelt werden. In diesem Beispiel wird nur 2 Stunden gewartet.

```csharp
IEnumerable<Event> InterruptAction()
{
    var waitFor = 2;
    var start = simulator.CurrentSimulationTime;
    Console.WriteLine($"Interrupted at {simulator.CurrentSimulationTime}: Waiting {waitFor} hours");
    yield return simulator.Timeout(TimeSpan.FromHours(waitFor));
    Console.WriteLine($"Waited {simulator.CurrentSimulationTime - start} hours.");
}

simulator.AddInterrupt(
  predicate: (process) =>
  {
      return true;
      // auch möglich bspw.: return process._machine.typeId == 2
  },
distribution: EXP(TimeSpan.FromHours(5)),
interruptAction: InterruptAction
);
```

Der in diesem Beispiel modellierte Stromausfall unterbricht alle Vorgänge, die in der Simulation ausgeführt werden. Die Dauer zwischen zwei Stromausfällen ist im Mittel 5 Stunden und wird durch eine exponentialverteilte Zufallsvariable modelliert. Die Unterbrechung wird durch die Methode ```InterruptAction``` behandelt. In diesem Beispiel wird nur 2 Stunden gewartet, bevor der Stromausfall vorbei ist und die Produktion fortgesetzt wird.

Als nächstes wird die Steuerung für die Simulation erstellt. Diese Steuerung erhält die zu simulierenden Arbeitsgänge, die Maschinen, die für die Produktion zur Verfügung stehen, den Planer, der die Arbeitsgänge plant und den Simulator, der die Produktion simuliert.

```csharp
var controller = new SimulationController(operations, machines, planner, simulator);
```

Die Steuerung soll auf verschiedene Events reagieren, die während der Simulation auftreten. Dazu gehören beispielsweise das Beenden von Vorgängen, das Unterbrechen von Vorgängen und das Neuplanen der übrigen Arbeitsgänge. 
Um die Reaktion auf diese Events zu implementieren wird ein EventHandler erstellt. Dabei wird in diesem Beispiel auf das ```ReplanningEvent``` und das ```OperationCompletedEvent``` reagiert. 

Tritt ein ```ReplanningEvent``` auf, werden die noch zu simulierenden Arbeitsgänge mit Hilfe des Planers neu geplant. 

Tritt ein ```OperationCompletedEvent``` auf, wird der entsprechende Arbeitsgang als abgeschlossen markiert und im Falle einer Verspätung werden die Start- und Endzeiten der Nachfolger des Arbeitsgangs entsprechend angepasst (Right Shift). Um den Right Shift rekursiv zu realisieren, werden zwei Hilfsmethoden verwendet, RightShiftSuccessors und UpdateSuccessorTimes.

```csharp
void RightShiftSuccessors(WorkOperation operation, List<WorkOperation> operationsToSimulate)
{
    var QueuedOperationsOnDelayedMachine = operationsToSimulate.Where(op => op.Machine == operation.Machine).OrderBy(op => op.EarliestStart).ToList();
    // Skip list till you find the current delayed operation, go one further and get the successor
    var successorOnMachine = QueuedOperationsOnDelayedMachine.SkipWhile(op => !op.Equals(operation)).Skip(1).FirstOrDefault();

    UpdateSuccessorTimes(operation, successorOnMachine, operationsToSimulate);
    UpdateSuccessorTimes(operation, operation.Successor, operationsToSimulate);
}

void UpdateSuccessorTimes(WorkOperation operation, WorkOperation? successor, List<WorkOperation> operationsToSimulate)
{
    if (successor == null) return;

    var delay = operation.LatestFinish - successor.EarliestStart;

    if (delay > TimeSpan.Zero)
    {
        successor.EarliestStart = successor.EarliestStart.Add(delay);
        successor.LatestStart = successor.LatestStart.Add(delay);
        successor.EarliestFinish = successor.EarliestFinish.Add(delay);
        successor.LatestFinish = successor.LatestFinish.Add(delay);

        RightShiftSuccessors(successor, operationsToSimulate);
    }
}

SimulationController.HandleSimulationEvent eHandler = (e,
                                                      planner,
                                                      simulation,
                                                      currentPlan,
                                                      operationsToSimulate,
                                                      finishedOperations) =>
{
    if (e is ReplanningEvent replanningEvent && operationsToSimulate.Any())
    {
        Console.WriteLine($"Replanning started at: {replanningEvent.CurrentDate}");
        var newPlan = planner.Schedule(operationsToSimulate
            .Where(op => !op.State.Equals(OperationState.InProgress)
                         && !op.State.Equals(OperationState.Completed))
            .ToList(), machines, replanningEvent.CurrentDate);
        controller.CurrentPlan = newPlan;
        simulation.SetCurrentPlan(newPlan.Operations);
    }
    if (e is OperationCompletedEvent operationCompletedEvent)
    {
        var completedOperation = operationCompletedEvent.CompletedOperation;

        // if it is too late, reschedule the current plan (right shift)
        var late = operationCompletedEvent.CurrentDate - completedOperation.LatestFinish;
        if (late > TimeSpan.Zero)
        {
            completedOperation.LatestFinish = operationCompletedEvent.CurrentDate;
            RightShiftSuccessors(completedOperation, operationsToSimulate);
        }
        if (!operationsToSimulate.Remove(completedOperation))
            throw new Exception($"Operation {completedOperation.WorkPlanPosition.Name} ({completedOperation.WorkOrder.Name}) " +
                $"was just completed but not found in the list of operations to simulate. This should not happen.");
        finishedOperations.Add(completedOperation);
        controller.FinishedOperations = finishedOperations;
    }
};
```

Dieser EventHandler wird nun in der Steuerung gesetzt. So wird das Verhalten der Steuerung definiert.

```csharp
controller.HandleEvent = eHandler;
```

Zu guter Letzt werden mit Hilfe der ```Execute```-Methode alle übergebenen Resourcen alloziert und die Simulation zu den gegebenen Parametern gestartet. Während die Simulation läuft, werden die Unterbrechungsaktionen ausgeführt, wenn die Bedingungen erfüllt sind. Der Verlauf der Simulation kann anhand des Simulationslogs auf der Konsole verfolgt werden.

```csharp
controller.Execute(TimeSpan.FromDays(7));
```

Die Logs der Simulation sehen beispielsweise wie folgt aus:

```
Replanning started at: 06.06.2023 07:09:56
Completed Lackieren at 06.06.2023 07:20:47 (lasted 01:44:43 - was planned 01:40:00)
Started Lackieren on machine 56ad699e-8d2d-4b88-ab75-7429c06eaa22 at 06.06.2023 07:20:47 (should have been at 06.06.2023 07:20:47).
Completed Lackieren at 06.06.2023 09:00:53 (lasted 01:40:06 - was planned 01:40:00)
Started Lackieren on machine 56ad699e-8d2d-4b88-ab75-7429c06eaa22 at 06.06.2023 09:00:53 (should have been at 06.06.2023 09:00:53).
Interrupted at 06.06.2023 09:33:37: Waiting 2 hours
Interrupted at 06.06.2023 09:33:37: Waiting 2 hours
``` 

Einige Ergebnisse und Statistiken der Simulation können mit Hilfe der ```GetResourceSummary```-Methode ausgegeben werden.
```csharp
Console.WriteLine(simulator.GetResourceSummary());
```


## Beispiel Anwendung in Python

Der Code für die Implementierung der Beispielanwendung in Python ist [hier](https://github.com/eshadepunkt/SimpleSimAdapters/blob/master/main.py) zu finden.

### Unterschiede zwischen Python und .NET <a id="unterschiede-python-net"></a>
Die Anwendung für Python ist prinzipiell identisch mit der in C#. Lediglich die Klassenbibliotheken der C#-Klassen müssen geladen werden, damit sie in Python genutzt werden können. Dazu wird das Modul ```pythonnet``` verwendet, welches es ermöglicht, .NET-Objekte in Python zu nutzen.

An einigen Stellen wurden eher python-typische Methoden verwendet, um mit Listen zu arbeiten (filter, sorted, dropwhile,...). Alternativ wäre auch eine Nutzung der gleichen Methoden wie in C# möglich, aber das führt zu längerem, komplizierterem und somit schwerer lesbarem Code.

Python-Funktionen, die als Delegate an die Simulationssoftware übergeben werden sollen, müssen vorher noch mit Hilfe des entsprechenden Konstruktors in eine entsprechende C#-Funktion umgewandelt werden.

### Ausführen der Anwendung
Das Python-Skript muss mit einem Argument ```-s``` bzw. ```--source```  aufgerufen werden, welches den Pfad zum Root-Ordner dieses Projektes enthält. Dieser Pfad wird benötigt, um die Klassenbibliotheken zu laden. Die entsprechenden .dll-Dateien sollten sich dann in einem Unterordner ```ProcessSimulator/bin/Debug/net6.0``` befinden.
Diese Dateien erhält man mit dem Befehl ```dotnet publish -c Debug```, ausgeführt im Ordner ```ProcessSimulator```.
