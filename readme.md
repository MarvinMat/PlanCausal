# Forschungs- und Entwicklungsprojekt "REPLAKI"

- [Einf�hrung](#einfuhrung)
  * [Ziel](#ziel)
  * [Anforderungen](#anforderungen)
  * [Kurzbeschreibung](#kurzbeschreibung)
- [Konfiguration](#konfiguration)
  * [Maschinen](#maschinen)
  * [Werkzeuge](#werkzeuge)
  * [Arbeitspl�ne](#arbeitsplane)
- [Beispiel Anwendung in .NET](#beispiel-anwendung-in-net)
- [Beispiel Anwendung in Python](#beispiel-anwendung-in-python)
  * [Unterschiede zwischen Python und .NET](#unterschiede-python-net)
  * [Ausf�hren der Anwendung](#ausfuhren-der-anwendung)

## Einf�hrung

### Ziel
Ziel dieser Anwendung ist es, dem Anwender die M�glichkeit zu geben, eine Produktion m�glichst realistisch zu simulieren.
Dabei soll der Anwender die M�glichkeit haben, die Produktion zu konfigurieren und diese anschlie�end simulieren zu lassen.
Es sollen dabei au�erdem zuf�llige Ereignisse und Unterbrechungen simuliert werden, so wie sie auch in der Realit�t auftreten w�rden.

Langfristig soll die Anwendung die Grundlage f�r ein Machine Learning Projekt sein, welches die Planung von Produktionsprozessen optimiert.
Aus diesem Grund bietet die Anwendung nach Abschluss der Simulation verschiedene Kennzahlen und Statistiken an, anhand derer der Erfolg der
Produktion gemessen und beurteilt werden kann.


### Anforderungen

- Die Anwendung soll in Python nutzbar sein.
- Es soll m�glich sein, einen vorgegebenen Produktionsplan zu simulieren.
- Es soll m�glich sein, einen Steuerungsalgorithmus vorzugeben, der auf Ereignisse, die w�hrend der Simulation auftreten, reagiert und 
entsprechende Anpassungen an dem aktuellen Produktionsplan vornimmt.
- Es soll m�glich sein, einen Planungsalgorithmus vorzugeben, der die zu simulierenden Vorg�nge plant und so die Simulation dieses Plans erm�glicht.
- Die Anwendung soll die M�glichkeit bieten, einen Produktionsprozess zu konfigurieren. Dabei sollen konfigurierbar sein:
    - Stammdaten der Produktion: 
        - Arbeitspl�ne
        - Maschinen
        - Werkzeuge
    - Zuf�llige Ereignisse, die beispielsweise f�r eine Unterbrechung einzelner Vorg�nge sorgen
    - Konkrete Produktionsauftr�ge, die simuliert werden sollen
- Die Anwendung soll die Ergebnisse der Simulation (in Form von verschiedenen Kennzahlen) zur Verf�gung stellen.
- Die Anwendung soll die M�glichkeit bieten, die Ergebnisse der Simulation zu visualisieren.

### Kurzbeschreibung

Die Anwendung besteht aus drei Hauptkomponenten, die im Folgenden kurz beschrieben werden.

Die erste Komponente ist der **Planer**. Dieser ist f�r die Planung der zu simulierenden Arbeitsg�nge zust�ndig. Er erh�lt die zu planenden Arbeitsg�nge und die Maschinen, auf denen diese ausgef�hrt werden sollen, und plant die Reihenfolge der Arbeitsg�nge. Dabei weist er ihnen Start- und Endzeitpunkt zu. Um eigene Planer-Implementierungen zu erm�glichen, k�nnen dieser von der abstrakten ```Planner```-Klasse abgeleitet werden. Eine Implementierung eines Planers, der auf dem Giffler-Thompson-Algorithmus basiert, ist bereits vorhanden (siehe [GifflerThompsonPlanner.cs](Planner.Implementation/GifflerThompsonPlanner.cs)).

Die zweite Komponente ist der **Simulator**. Dieser erh�lt die vom Planer geplanten Arbeitsg�nge und simuliert diese. Dabei werden die Arbeitsg�nge auf den Maschinen ausgef�hrt und die R�stzeiten zwischen den Arbeitsg�ngen ber�cksichtigt. Au�erdem werden zuf�llige Ereignisse simuliert, die beispielsweise f�r eine Unterbrechung einzelner Arbeitsg�nge sorgen.

Die dritte Komponente ist die **Steuerung**. Sie ist die Verbindung zwischen den beiden anderen Komponenten. Die Steuerung erh�lt die vom Simulator geworfenen Ereignisse und reagiert auf diese. Dabei kann sie beispielsweise die Reihenfolge der Arbeitsg�nge �ndern oder einzelne Arbeitsg�nge auf andere Maschinen verschieben. Regelm��ig wird von der Simulation ein Neuplanungs-Event ausgel�st, welches von der Steuerung behandelt wird. Dabei kann sie den Planer aufrufen und die Planung der Arbeitsg�nge neu ansto�en. Der Planer plant dann die Arbeitsg�nge neu, der neue Plan wird von der Steuerung an den Simulator �bergeben und die Simulation wird fortgesetzt.

![image](doc/Diagramme/Sequenzdiagramm.svg)

Ein ausf�hrliches Beispiel, wie diese Software zu verwenden ist, inklusive detaillierter Erkl�rungen folgt weiter unten (siehe [Beispiel Anwendung in .NET](#beispiel-anwendung-in-net) bzw. [Beispiel Anwendung in Python](#beispiel-anwendung-in-python)).

## Konfiguration
Die Konfigurierbarkeit der Applikation ist ein zentrales Features und ist f�r die Komplexit�t des zugrundeliegenden Problems von gro�er Bedeutung. Die Konfiguration der Stammdaten erfolgt �ber JSON-Dateien, die die verschiedenen Ressourcen und Parameter enthalten. Die Konfigurationen werden in den folgenden Abschnitten genauer beschrieben.

### Maschinen
Die in der Produktion vorhandenen Maschinen werden anhand ihres entsprechenden Typs konfiguriert. Dabei wird vereinfacht angenommen, dass jede Maschine eines Typs die gleichen Eigenschaften besitzt. F�r jeden Maschinentyp sind folgende Eigenschaften konfigurierbar:
- ```typeId```: eine eindeutige ID
- ```count```: die Anzahl der vorhandenen Maschinen dieses Typs
- ```name```: ein Name
- ```allowedToolIds```: eine Liste von auf diesem Maschinentyp erlaubten Werkzeugen (dabei werden die Typ-Ids der jeweiligen Werkzeuge angegeben, siehe [Werkzeuge](#werkzeuge))
- ```changeoverTimes```: eine R�stzeitmatrix. Diese ist wie folgt aufgebaut: Das Element in Zeile x und Spalte y enth�lt die R�stzeit in Minuten, die ben�tigt wird, um auf diesem Maschinentyp von dem Werkzeug an der x-ten Stelle im ```allowedToolIds```-Array umzur�sten auf das Werkzeug an der y-ten Stelle in dem Array. In den Diagonalelementen (Zeile z, Spalte z) der Matrix k�nnen somit auch R�st- oder Vorbereitungszeiten angegeben werden, die bei der Nutzung eines bestimmten Werkzeugs vor jedem Arbeitsgang anfallen.
Ein Beispiel ist [hier](#rustzeitbsp) genauer beschrieben.

Die Maschinen werden in der Datei [machines.json](Machines.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel f�r die Konfiguration einer Maschine.

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

Die R�stzeit-Matrix enth�lt unter anderem folgende Angaben: In der ersten Zeile und zweiten Spalte sind 5 Minuten R�stzeit angegeben.
Das bedeutet, dass der Wechsel vom ersten Werkzeug im ```allowedToolIds```-Array (Werkzeug 1) zum zweiten Werkzeug in diesem Array (Werkzeug 3) durchschnittlich 5 Minuten dauert.
In Zeile 2, Spalte 3 ist angegeben, dass der Wechsel von Werkzeug 3 (2-tes Werkzeug im ```allowedToolIds```-Array) zu Werkzeug 4 (3-tes Werkzeug im ```allowedToolIds```-Array) 10 Minuten dauert. Au�erdem ist im dritten Diagonalelement (Zeile 3, Spalte 3) angegeben, dass vor jedem Arbeitsgang mit dem dritten Werkzeug (Werkzeug 4) 2 Minuten R�st- bzw. Vorbereitungszeit ben�tigt werden.

In diesem Beispiel ist die Matrix symmetrisch, dies ist aber nicht notwendigerweise der Fall.

### Werkzeuge
Werkzeuge werden ebenfalls anhand ihres Typs konfiguriert. Ein Werkzeugtyp kann dabei ein bestimmtes tats�chlich existierendes Werkzeug beschreiben, aber kann auch einen bestimmten Operationsmodus einer Maschine abbilden. 
F�r ein Werkzeug sind folgende Eigenschaften konfigurierbar:
- ```typeId```: eine eindeutige ID
- ```name```: ein Name
- ```description```: eine Beschreibung.

Die Werkzeuge werden in der Datei [tools.json](Tools.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel f�r die Konfiguration eines Werkzeugs.

```json
{
    "typeId": 1,
    "name": "Tool 1",
    "description": "Tool 1"
}
```

### Arbeitspl�ne
Arbeitspl�ne sind die dem Produktionsprozess zugrunde liegenden Stammdaten. Sie beschreiben, welche Arbeitsschritte notwendig sind, um ein bestimmtes Produkt herzustellen. 
Die Arbeitspl�ne werden dabei als eine Liste von Arbeitsg�ngen (alternativ: Arbeitsplanpositionen) beschrieben. F�r jeden Arbeitsplan sind die folgenden Eigenschaften konfigurierbar:
- ```workPlanId```: eine eindeutige Id
- ```name```: der Name des Produkts
- ```operations```: ein Array von zugeh�rigen Arbeitsg�ngen. Jeder einzelne Arbeitsgang ist dabei wieder ein Objekt, f�r welches folgende Eigenschaften konfigurierbar sind:
    - ```name```: der Name
    - ```duration```: die Bearbeitungszeit (ohne R�sten) in Minuten
    - ```machineId```: die ID des Maschinentyps, auf dem dieser Arbeitsgang ausgef�hrt werden soll
    - ```toolId```: die ID des zu verwendenden Werkzeugtyps.
    
Die Arbeitspl�ne werden in der Datei [workplans.json](WorkPlans.json) konfiguriert.
Der folgende Ausschnitt zeigt ein Beispiel f�r die Konfiguration eines Arbeitsplans.

```json
{
    "workPlanId": 1,
    "name": "Tisch"
    "operations": [
        {
            "machineId": 1,
            "duration": 15,
            "name": "Tischbein s�gen",
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

Zun�chst werden die ben�tigten Ressourcen wie Maschinen, Werkzeuge und Arbeitspl�ne aus JSON-Dateien geladen. Diese Ressourcen dienen als Grundlage f�r die Simulation und Planung der Produktion.

```csharp
IMachineProvider machineProvider = new MachineProviderJson("../../../../Machines.json");
var machines = machineProvider.Load();

IToolProvider toolProvider = new ToolProviderJson("../../../../Tools.json");
var tools = toolProvider.Load();

IWorkPlanProvider workPlanProvider = new WorkPlanProviderJson("../../../../WorkPlans.json");
var plans = workPlanProvider.Load();
```

Nachdem die Ressourcen geladen wurden, wird f�r jeden Arbeitsplan (also jedes Produkt) ein Produktionsauftrag erstellt, der simuliert werden soll. Dabei wird angegeben wie viele Einheiten des Produkts hergestellt werden sollen (```Quantity```).

```csharp
var orders = plans.Select(plan => new ProductionOrder()
{
    Name = $"Order {plan.Name}",
    Quantity = 60,
    WorkPlan = plan,
}).ToList();
```

Anschlie�end werden aus den Auftr�gen die entsprechenden ```WorkOperation```-Objekte erstellt. Diese Objekte enthalten Informationen �ber die einzelnen Arbeitsg�nge und deren Abh�ngigkeiten.
Die Methode ```ModelUtil.GetWorkOperationsFromOrders``` �bernimmt das Erstellen der einzelnen ```WorkOperation```-Objekte und verkn�pft diese untereinander.
Jedes ```WorkOperation```-Objekt kennt seinen Vorg�nger und seinen Nachfolger, falls diese existieren.

```csharp
var operations = ModelUtil.GetWorkOperationsFromOrders(orders);
```

Als n�chstes wird ein Simulator erstellt, der einen Seed f�r die Zufallszahlengenerierung erh�lt, sowie ein Startdatum der Simulation. Diese Klasse �bernimmt das Simulieren der Produktion.

```csharp
var simulator = new Simulator(rnd.Next(), DateTime.Now);
```

F�r die Simulation der Produktion wird im n�chsten Schritt eine zuf�llige Unterbrechung hinzugef�gt. Beispielhaft wird hier ein Stromausfall modelliert, der Vorg�nge auf allen Maschinen unterbricht.
Die Methode ```AddInterrupt``` erh�lt als ersten Parameter eine Funktion, die bestimmt, ob ein Vorgang unterbrochen werden soll. Sie wird f�r jeden Prozess in der Simulation aufgerufen und gibt einen Wahrheitswert zur�ck, der bestimmt, ob dieser Prozess unterbrochen werden soll. In diesem Beispiel wird jeder Vorgang unterbrochen, m�glich w�re es aber beispielsweise auch, nur Vorg�nge auf einer bestimmten Maschine oder einem bestimmten Maschinentyp zu unterbrechen.
Als zweiten Parameter erh�lt die Methode eine Verteilung, die bestimmt, wie lange die Unterbrechung dauert. In diesem Beispiel wird eine exponentialverteilte Zufallsvariable mit einer mittleren Dauer von 5 Stunden verwendet.
Als dritten Parameter erh�lt die Methode eine Funktion, die ausgef�hrt wird, wenn ein Vorgang unterbrochen wird. Dabei kann beispielsweise die Unterbrechung behandelt werden. In diesem Beispiel wird nur 2 Stunden gewartet.

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
      // auch m�glich bspw.: return process._machine.typeId == 2
  },
distribution: EXP(TimeSpan.FromHours(5)),
interruptAction: InterruptAction
);
```

Der in diesem Beispiel modellierte Stromausfall unterbricht alle Vorg�nge, die in der Simulation ausgef�hrt werden. Die Dauer zwischen zwei Stromausf�llen ist im Mittel 5 Stunden und wird durch eine exponentialverteilte Zufallsvariable modelliert. Die Unterbrechung wird durch die Methode ```InterruptAction``` behandelt. In diesem Beispiel wird nur 2 Stunden gewartet, bevor der Stromausfall vorbei ist und die Produktion fortgesetzt wird.

Als n�chstes wird die Steuerung f�r die Simulation erstellt. Diese Steuerung erh�lt die zu simulierenden Arbeitsg�nge, die Maschinen, die f�r die Produktion zur Verf�gung stehen, den Planer, der die Arbeitsg�nge plant und den Simulator, der die Produktion simuliert.

```csharp
var controller = new SimulationController(operations, machines, planner, simulator);
```

Die Steuerung soll auf verschiedene Events reagieren, die w�hrend der Simulation auftreten. Dazu geh�ren beispielsweise das Beenden von Vorg�ngen, das Unterbrechen von Vorg�ngen und das Neuplanen der �brigen Arbeitsg�nge. 
Um die Reaktion auf diese Events zu implementieren wird ein EventHandler erstellt. Dabei wird in diesem Beispiel auf das ```ReplanningEvent``` und das ```OperationCompletedEvent``` reagiert. 

Tritt ein ```ReplanningEvent``` auf, werden die noch zu simulierenden Arbeitsg�nge mit Hilfe des Planers neu geplant. 

Tritt ein ```OperationCompletedEvent``` auf, wird der entsprechende Arbeitsgang als abgeschlossen markiert und im Falle einer Versp�tung werden die Start- und Endzeiten der Nachfolger des Arbeitsgangs entsprechend angepasst (Right Shift). Um den Right Shift rekursiv zu realisieren, werden zwei Hilfsmethoden verwendet, RightShiftSuccessors und UpdateSuccessorTimes.

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

Zu guter Letzt werden mit Hilfe der ```Execute```-Methode alle �bergebenen Resourcen alloziert und die Simulation zu den gegebenen Parametern gestartet. W�hrend die Simulation l�uft, werden die Unterbrechungsaktionen ausgef�hrt, wenn die Bedingungen erf�llt sind. Der Verlauf der Simulation kann anhand des Simulationslogs auf der Konsole verfolgt werden.

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

Einige Ergebnisse und Statistiken der Simulation k�nnen mit Hilfe der ```GetResourceSummary```-Methode ausgegeben werden.
```csharp
Console.WriteLine(simulator.GetResourceSummary());
```


## Beispiel Anwendung in Python

Der Code f�r die Implementierung der Beispielanwendung in Python ist [hier](https://github.com/eshadepunkt/SimpleSimAdapters/blob/master/main.py) zu finden.

### Unterschiede zwischen Python und .NET <a id="unterschiede-python-net"></a>
Die Anwendung f�r Python ist prinzipiell identisch mit der in C#. Lediglich die Klassenbibliotheken der C#-Klassen m�ssen geladen werden, damit sie in Python genutzt werden k�nnen. Dazu wird das Modul ```pythonnet``` verwendet, welches es erm�glicht, .NET-Objekte in Python zu nutzen.

An einigen Stellen wurden eher python-typische Methoden verwendet, um mit Listen zu arbeiten (filter, sorted, dropwhile,...). Alternativ w�re auch eine Nutzung der gleichen Methoden wie in C# m�glich, aber das f�hrt zu l�ngerem, komplizierterem und somit schwerer lesbarem Code.

Python-Funktionen, die als Delegate an die Simulationssoftware �bergeben werden sollen, m�ssen vorher noch mit Hilfe des entsprechenden Konstruktors in eine entsprechende C#-Funktion umgewandelt werden.

### Ausf�hren der Anwendung
Das Python-Skript muss mit einem Argument ```-s``` bzw. ```--source```  aufgerufen werden, welches den Pfad zum Root-Ordner dieses Projektes enth�lt. Dieser Pfad wird ben�tigt, um die Klassenbibliotheken zu laden. Die entsprechenden .dll-Dateien sollten sich dann in einem Unterordner ```ProcessSimulator/bin/Debug/net6.0``` befinden.
Diese Dateien erh�lt man mit dem Befehl ```dotnet publish -c Debug```, ausgef�hrt im Ordner ```ProcessSimulator```.
