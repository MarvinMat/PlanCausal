# Forschungs- und Entwicklungsprojekt "REPLAKI"

1. [Einführung](#einführung)
   - [Ziel](#ziel)
   - [Anforderungen](#anforderungen)
   - [Kurzbeschreibung](#kurzbeschreibung)
2. [Konfiguration](#konfiguration)
   - [Maschinen](#maschinen)
   - [Werkzeuge](#werkzeuge)
   - [Arbeitspläne](#arbeitspläne)
3. [Beispiel Anwendung in .NET](#beispiel-anwendung-in-net)
4. [Beispiel Anwendung in Python](#beispiel-anwendung-in-python)
   - [Python-Anwendung im Detail](#python-anwendung-im-detail)
   - [Unterschiede zwischen Python und .NET](#unterschiede-zwischen-python-und-net)
   - [Ausführen der Anwendung](#ausführen-der-anwendung)

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
- ```variationCoefficient```: Ein Koeffizient der das Grundrauschen der Bearbeitungszeiten der Arbeitsgänge beschreibt. Dieser Wert wird verwendet, um die Bearbeitungszeiten der Arbeitsgänge zufällig zu variieren.
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
            "variationCoefficient": 0.2,
            "name": "Tischbein sägen",
            "toolId": 2
        },
        {
            "machineId": 2,
            "duration": 10,
            "variationCoefficient": 0.1,
            "name": "Tischbein schleifen",
            "toolId": 1
        },
        {
            "machineId": 3,
            "duration": 5,
            "variationCoefficient": 0.3,
            "name": "Tischbein lackieren",
            "toolId": 3
        }
    ]
},
```
## Klassendiagramm der wichtigsten Klassen

![image](doc/Diagramme/Klassendiagramm.svg)



## Beispiel Anwendung in .NET <a id="beispiel-anwendung-in-net"></a>
Der in der Datei [Main.cs](ProcessSimulator/Main.cs) vorliegende Code implementiert eine beispielhafte Anwendung der Simulation zur Produktionsplanung und -steuerung. Im Folgenden wird der Aufbau und die Funktionsweise beschrieben:

Grundlegend wird für das Starten der Simulation zwei Dinge benötigt. Ein [Szenario](ProcessSimulator/Scenarios/ProductionScenario), 
praktisch eine Konfiguration der äußeren Umstände, beschreibt die Zusammenhänge von Controller, Planer und Simulator sowie eine Instanz eines Loggers
werden benötigt. Wir haben uns für die Benutzung von ```Serilog``` entschieden. Hier eine beispielhafte Konfiguration,
die ein fortlaufendes Logfile erzeugt und die Lognachrichten zusätzlich in der Konsole ausgibt:
  
```csharp
Log.Logger = new LoggerConfiguration()
.WriteTo.Console()
.WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
.MinimumLevel.Information()
.Enrich.FromLogContext()
.CreateLogger();
```

### Beispielhafte Konfiguration

Im folgenden Absatz soll die Benutzung des [ProductionScenarios](ProcessSimulator/Scenarios/ProductionScenario) näher 
erläutert werden. Ein ProductionScenario ist eine konkrete Implementierung des [IScenario](ProcessSimAbstraction/IScenario) 
Interfaces. Das bereitgestellte ProductionScenario implentiert bereits ein breitgefächterte public API, die es ermöglicht
einfach und schnell eine Simulation zu konfigurieren und zu starten. Es folgt eine beispielhafte Konfiguration:

```csharp
var scenario = new ProductionScenario("ElevenMachinesProblem", "Test")
{
    Duration = TimeSpan.FromDays(30),
    Seed = 42,
    RePlanningInterval = TimeSpan.FromHours(8),
    StartTime = DateTime.Now,
    InitialCustomerOrdersGenerated = 5
}
    .WithEntityLoader(new MachineProviderJson($"../../../../Machines_11Machines.json"))
    .WithEntityLoader(new WorkPlanProviderJson($"../../../../Workplans_11Machines.json"))
    .WithEntityLoader(new CustomerProviderJson("../../../../Customers.json"))
    .WithInterrupt(predicate: process => ((MachineModel)process).Machine.MachineType is 1 or 11, distribution:
        CoreAbstraction.Distributions.ConstantDistribution(TimeSpan.FromHours(4)), interruptAction: InterruptAction)
    .WithOrderGenerationFrequency(
        CoreAbstraction.Distributions.DiscreteDistribution(
            new List<TimeSpan> { TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30) }, 
            new List<double> { 0.25, 0.60, 0.15 }
        )
    )
    .WithReporting(".");
```
Die hier gezeigte Implementierung ist ein Beispiel für die fließende API des ProductionScenarios. Durch Method Chaining
wird eine intuitive und ausdrucksstarke Konfiguration ermöglicht. Im Folgenden werden die Funktionen der einzelnen 
Methoden und Konstrukte erläutert.

#### Detailierter Aufbau der Konfiguration

1. **Erstellung eines Szenarios:**
   ```csharp
   var scenario = new ProductionScenario("ElevenMachinesProblem", "Test")
   ```
   Hier wird ein neues `ProductionScenario` Objekt erstellt. Die Argumente `"ElevenMachinesProblem"` und `"Test"` sind beispielhaft der Name und die Beschreibung des Szenarios.

2. **Konfiguration des Szenarios:**
   Die folgenden Zeilen konfigurieren das Szenario durch das Setzen verschiedener Eigenschaften:
  - `Duration = TimeSpan.FromDays(30),` legt die Dauer des Szenarios auf 30 Tage fest.
  - `Seed = 42,` setzt einen Seed-Wert für Zufallszahlengeneratoren.
  - `RePlanningInterval = TimeSpan.FromHours(8),` definiert das Intervall für Neuplanungen (alle 8 Stunden).
  - `StartTime = DateTime.Now,` setzt den Startzeitpunkt auf die aktuelle Zeit.
  - `InitialCustomerOrdersGenerated = 5` gibt an, dass zu Beginn des Szenarios 5 Kundenbestellungen generiert werden.

3. **Einbindung externer Ressourcen:**
   Mit `.WithEntityLoader(...)` werden verschiedene externe Ressourcen geladen:
  - `new MachineProviderJson($"../../../../Machines_11Machines.json")` lädt Maschinendaten aus einer JSON-Datei.
  - `new WorkPlanProviderJson($"../../../../Workplans_11Machines.json")` und 
  - `new CustomerProviderJson("../../../../Customers.json")` laden Arbeitspläne bzw. Kundeninformationen aus JSON-Dateien.

4. **Unterbrechungslogik:**
   `.WithInterrupt(...)` definiert eine Unterbrechungslogik für bestimmte Prozesse:
  - `predicate: process => ((MachineModel)process).Machine.MachineType is 1 or 11` gibt ein Prädikat an, das bestimmt, welche Maschinen unterbrochen werden (hier Maschinentypen 1 oder 11).
  - `distribution: CoreAbstraction.Distributions.ConstantDistribution(TimeSpan.FromHours(4))` definiert die Dauer der Unterbrechung.
  - `interruptAction: InterruptAction` legt die Aktion fest, die bei einer Unterbrechung ausgeführt wird.

5. **Generierung von Bestellungen:**
   `.WithOrderGenerationFrequency(...)` konfiguriert, wie oft neue Aufträge generiert werden:
  - Es wird eine diskrete Verteilung mit verschiedenen Zeitintervallen (`TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30)`) und zugehörigen Wahrscheinlichkeiten (`0.25, 0.60, 0.15`) definiert.

6. **Reporting:**
   `.WithReporting(".")`ermöglicht die Berichterstattung bzw. Logging, wobei `"."` den Pfad des Berichts angibt.

Für die oben beschriebene Unterbrechungslogik wird ein Callback benötigt, also genau die Methode, die bei einer Unterbrechung ausgeführt wird. Diese Methode wird im folgenden Absatz erläutert.

```csharp
IEnumerable<Event> InterruptAction(ActiveObject<Simulation> simProcess, IScenario productionScenario)
{
    if (productionScenario is not ProductionScenario prodScenario)
        throw new NullReferenceException("Scenario is null.");
    if (prodScenario.Simulator is not Simulator simulator)
        throw new NullReferenceException("Simulator is null.");
    
    if (simProcess is MachineModel machineModel)
    {
        var waitFor = POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)));
        var start = simulator.CurrentSimulationTime;

        Log.Logger.Warning("Interrupted {Machine} at {Time}",
            machineModel.Machine.Description, simulator.CurrentSimulationTime);
        yield return simulator.Timeout(waitFor);
        Log.Logger.Warning("{Machine} waited {Waited} hours (done at {Time})",
            machineModel.Machine.Description, simulator.CurrentSimulationTime - start, simulator.CurrentSimulationTime);
    }
}
```
Die gezeigte Methode `InterruptAction` ist eine Callback-Funktion. Sie nimmt zwei Parameter entgegen: 
- `simProcess` : ein `ActiveObject<Simulation>` Typ 
- `productionScenario` : ein [`IScenario`](ProcessSimAbstraction/IScenario) Typ. 

Die Methode gibt ein `IEnumerable<Event>` zurück.

##### Detailierter Aufbau der Callback-Funktion `InterruptAction`

1. **Parameter-Prüfung:**
  - Die Methode überprüft zuerst, ob `productionScenario` tatsächlich eine Instanz von `ProductionScenario` ist. Wenn dies nicht der Fall ist, wird eine `NullReferenceException` mit der Nachricht "Scenario is null." ausgelöst.
  - Dann wird geprüft, ob das `prodScenario`-Objekt einen gültigen `Simulator` hat. Ist dies nicht der Fall, wird ebenfalls eine `NullReferenceException` mit der Nachricht "Simulator is null." ausgelöst.

2. **Verarbeitungslogik für `MachineModel`:**
  - Die Methode prüft, ob `simProcess` eine Instanz von `MachineModel` ist. Wenn ja, wird die Logik für das `MachineModel` ausgeführt.
  - Innerhalb dieses Blocks wird eine Wartezeit berechnet. Dies geschieht durch den Aufruf `POS(N(TimeSpan.FromHours(2), TimeSpan.FromMinutes(30)))`, der eine stochastische Berechnung mit einer normalverteilten Zufallsvariablen darstellt, mit einem Mittelwert von 2 Stunden und einer Standardabweichung von 30 Minuten.
  - Es wird der Startzeitpunkt der Unterbrechung mit `simulator.CurrentSimulationTime` festgehalten.

3. **Logging:**
  - Es erfolgt ein Log-Eintrag, der darauf hinweist, dass eine bestimmte Maschine zu einem bestimmten Zeitpunkt unterbrochen wurde.
  - Nach dem Ablauf der Wartezeit (simuliert durch `yield return simulator.Timeout(waitFor)`) erfolgt ein weiterer Log-Eintrag, der anzeigt, wie lange die Maschine gewartet hat und zu welcher Zeit die Wartezeit beendet wurde.

4. **Verwendung von `yield return`:**
  - Durch die Verwendung von `yield return` innerhalb der `if`-Anweisung gibt die Methode ein `Event` zurück, das den Zeitpunkt markiert, an dem die Maschine ihre Wartezeit beendet. Dies ist typisch für C#-Methoden, die `IEnumerable` zurückgeben und ist ein Mechanismus, um eine Sequenz von Werten über mehrere Aufrufe hinweg zu erzeugen.

###### TL;DR

Zusammengefasst ist `InterruptAction` eine Methode, die im Kontext einer Simulation eine Unterbrechung einer Maschine verarbeitet. Sie führt Sicherheitsüberprüfungen durch, berechnet Wartezeiten, protokolliert Ereignisse und gibt ein Ereignis zurück, das den Abschluss der Unterbrechung signalisiert.

#### Starten der Simulation

Wenn ein IScenario Objekt erstellt und konfiguriert wurde, kann es mit `.Run()` gestartet werden. Beispiel:

```csharp
scenario.Run();
scenario.CollectStats();
```

Die hier gezeigte ```CollectStats``` Methode sammelt die Statistiken der Simulation und gibt sie in der Konsole aus.

Abschließend wird mit ```Log.CloseAndFlush()``` die Logfile geschlossen und alle belegten Resourcen des Loggers freigegeben. 

### Zusammenfassung

Der gesamte Prozess der Initialisierung und Ausführung der Simulation kann wie folgt zusammengefasst werden:

1. Erstellen eines Szenarios das den IScenario Typ implementiert.
2. Konfigurieren des Szenarios durch Setzen verschiedener Eigenschaften.
3. Einbinden externer Ressourcen.
4. eventuell Definieren einer Unterbrechungslogik.
5. eventuell Definieren einer Neuplanungslogik.
6. Starten der Simulation durch Aufruf der Methode `.Run()` des Szenarios.
7. Sammeln der Statistiken durch Aufruf der Methode `.CollectStats()` des Szenarios - sofern implementiert.

## Beispiel-Anwendung in Python

Der Code für die Implementierung der Beispielanwendung in Python ist im Ordner ```examples``` in der Datei [main.py](examples/main.py) zu finden.

Zuerst wird die Python-Anwendung detailliert erklärt, wobei aber genauere Ausführungen zu den einzelnen Methoden und Klassen ausgelassen werden, da diese bereits im .NET-Beispiel ausführlich erläutert wurden. Anschließend werden die Unterschiede der Beispielanwendungen in Python und .NET genauer beleuchtet. Zum Schluss folgen einige Hinweise dazu, wie die Python-Anwendung ausgeführt werden kann.

### Python-Anwendung im Detail

Zuerst werden einige Python-Bibliotheken geladen sowie die .Net Core CLR. Diese wird benötigt, um die .Net Core Klassenbibliotheken laden zu können.

```python
import os
import argparse

from pythonnet import load

load("coreclr")
import clr
```

Als nächstes wird ein Kommandozeilenargument (```-s``` bzw. ```--source```) eingelesen, welches den Pfad zum Root-Ordner dieses Projektes und somit zu den .dll-Dateien der Klassenbibliotheken enthält.

```python
parser = argparse.ArgumentParser(description='Run the Process Simulation with Python')
parser.add_argument('-s', '--source', type=str, default=os.getcwd(), help='The root directory of the source code')
args = parser.parse_args()

root_dir = args.source

bin_dir = os.path.join(root_dir, 'ProcessSimulator\\bin\\Debug\\net6.0')
```

Nun werden die .dll-Dateien geladen, die für die Simulation benötigt werden.
```python
dll_files = []
for file in os.listdir(bin_dir):
    if file.endswith('.dll'):
        dll_files.append(os.path.join(bin_dir, file))

for lib in dll_files:
    clr.AddReference(lib)
```

Die benötigten Klassen können nun importiert werden.
```python
from System import TimeSpan
from System import DateTime
from System import Double
from System.Collections.Generic import List 
from System.Collections.Generic import *

from System import *

from Serilog import *

from SimSharp import ActiveObject, Simulation, Event

from Core.Implementation.Services import MachineProviderJson
from Core.Implementation.Services import WorkPlanProviderJson
from Core.Implementation.Services import CustomerProviderJson
from Core.Abstraction.Services import PythonGeneratorAdapter
from Core.Abstraction.Domain.Processes import Plan, WorkOperation
from Core.Abstraction.Domain.Resources import Machine

from Core.Abstraction import Distributions

from Planner.Implementation import PythonDelegatePlanner

from ProcessSim.Implementation import Simulator
from ProcessSim.Implementation.Core.SimulationModels import MachineModel

from ProcessSimulator.Scenarios import ProductionScenario
```

Zu Beginn wird das Logging konfiguriert. In der Simulationsbibliothek wurde Serilog verwendet, um das Logging zu realisieren. Diese Bibliothek kann auch in Python verwendet werden, sie wurde oben importiert. Es wird nun ein Logger erstellt, der die Log-Nachrichten in die Konsole schreibt. Außerdem wird das Log-Level auf Information gesetzt, sodass alle Log-Nachrichten mit diesem Level oder höher in die Konsole geschrieben werden. Auch möglich wäre es, einen Logger zu erstellen, der die Log-Nachrichten in eine Datei schreibt.

```python
logger_configuration = LoggerConfiguration() \
    .MinimumLevel.Information() \
    .Enrich.FromLogContext() \

logger_configuration =  ConsoleLoggerConfigurationExtensions.Console(logger_configuration.WriteTo)
# logger_configuration = ConsoleLoggerConfigurationExtensions.File(logger_configuration.WriteTo, "log.txt", rollingInterval=RollingInterval.Day)

Log.Logger = logger_configuration.CreateLogger()
```

Nun werden die Pfade für die Konfigurationsdateien der Maschinen, Arbeitspläne und Kunden hinterlegt.

```python
path_machines = os.path.join(root_dir, 'Machines_11Machines.json')
path_workplans = os.path.join(root_dir, 'Workplans_11Machines.json')
path_customers = os.path.join(root_dir, 'customers.json')
```

Als nächstes werden Listen mit Zeitspannen und die dazugehörigen Wahrscheinlichkeiten angelegt. Diese werden später dazu verwendet, anzugeben, wie oft ein neuer Auftrag in der Simulation generiert wird. Da später C#-Listen verwendet werden müssen, werden an dieser Stelle gleich die Python-Listen in C#-Listen umgewandelt.

```python
timespans_py = [TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(30)]
timespans_net = List[TimeSpan]()
prob_py = [0.25, 0.60, 0.15]
prob_net = List[Double]()

for item in prob_py:
    prob_net.Add(item)

for item in timespans_py:
    timespans_net.Add(item)
```

Nun wird eine Methode definiert, die die Implementierung eines eigenen Planungsalgorithmus in Python veranschaulichen soll. Diese Methode erhält eine Liste von Arbeitsgängen, eine Liste von Maschinen und ein Datum, an dem die Planung beginnen soll. Die Methode gibt einen Plan zurück, der die geplanten Arbeitsgänge enthält. 

In diesem Beispiel loggt die Methode nur, dass sie aufgerufen wurde und gibt einen leeren Plan zurück.

```python
def schedule_internal(work_operations : List[WorkOperation], machines : List[Machine], current_time : DateTime):
    Log.Logger.Information(F"Scheduling: {work_operations.Count} operations on {machines.Count} machines at {current_time}.")
    return Plan(List[WorkOperation](), False)
```

Nun wird das ```ProductionScenario```-Objekt erstellt, welches die Simulation steuert. Mit Hilfe dieses Objektes kann die gesamte Simulation konfiguriert und ausgeführt werden. 

Beispielhaft werden hier die ```EntityLoader``` für Maschinen, Arbeitspläne und Kunden gesetzt. Diese werden dann im Szenario verwendet, um die entsprechenden Ressourcen aus den JSON-Dateien zu laden.

Außerdem wird eine Unterbrechung registriert. Dabei werden alle Maschinen aller 5 Stunden unterbrochen. Was bei der Unterbrechung auf jeder Maschine passiert, wird in der Methode ```interrupt_action``` weiter unten definiert.

Weiterhin wird die Häufigkeit der Generierung von neuen Aufträgen gesetzt. In diesem Beispiel folgt die Dauer zwischen zwei Aufträgen einer diskreten Verteilung mit den bereits oben konfigurierten Werten und Wahrscheinlichkeiten.

Als nächstes wird ein eigener, in Python implementierter Planer gesetzt. Dazu wird die Klasse ```PythonDelegatePlanner``` verwendet, die eine Python-Funktion als Argument erhält. Hier wird die oben definierte Funktion ```schedule_internal``` verwendet.

Zuletzt wird noch ein Reporting-Verzeichnis gesetzt. In diesem Beispiel wird das aktuelle Verzeichnis verwendet, in dem die Python-Anwendung ausgeführt wird.
In diesem Verzeichnis werden nach dem Ende der Simulation einige Dateien erstellt, die die Ergebnisse und Daten der Simulation enthalten.

```python
scenario = ProductionScenario("Python-11-Machines-Problem", "Simulating the 11-machine problem using python and .NET")\
    .WithEntityLoader(MachineProviderJson(path_machines))\
    .WithEntityLoader(WorkPlanProviderJson(path_workplans))\
    .WithEntityLoader(CustomerProviderJson(path_customers))\
    .WithInterrupt(
        predicate= Func[ActiveObject[Simulation], bool](lambda process: True), 
        distribution= Distributions.ConstantDistribution(TimeSpan.FromHours(5)),
        interruptAction= Func[ActiveObject[Simulation], ProductionScenario, IEnumerable[Event]](
            lambda simObject, scenario: \
                PythonGeneratorAdapter[Event](PythonEnumerator(interrupt_action, simObject, scenario))
            ))\
    .WithOrderGenerationFrequency(Distributions.DiscreteDistribution[TimeSpan](
        timespans_net, prob_net))\
    .WithPlanner(PythonDelegatePlanner(schedule_internal))
    .WithReporting(".")
```

Weiterhin werden einige Properties des Szenarios gesetzt. Hier wird die zu simulierende Dauer der Simulation, der Seed, das Intervall, in dem regelmäßig neu geplant werden soll, die Startzeit und die Anzahl der initial zu simulierenden Aufträge festgelegt.

```python
scenario.Duration = TimeSpan.FromDays(1)
scenario.Seed = 42
scenario.RePlanningInterval = TimeSpan.FromHours(8)
scenario.StartTime = DateTime.Now
scenario.InitialCustomerOrdersGenerated = 5
```

Danach folgt die Definition der Unterbrechung. Dafür wird eine Python-Generator-Funktion verwendet. Da diese aber anders funktioniert als in .NET, müssen hier noch eigene Klassen verwendet werden, die die gleiche Funktionalität ermöglichen, u. a. die PythonEnumerator-Klasse.

In diesem Beispiel wird während der Unterbrechung einfach 2 Stunden gewartet, bevor die Produktion fortgesetzt wird.

```python
def interrupt_action(sim_process, prod_scenario):
    if isinstance(prod_scenario.Simulator, Simulator):
        simulator = prod_scenario.Simulator
    else:
        raise Exception("Simulator is not of type Simulator")
    
    if isinstance(sim_process, MachineModel):
        waitFor = 2
        start = simulator.CurrentSimulationTime
        Log.Logger.Warning(F"Interrupted Machine {sim_process.Machine.Description} at {simulator.CurrentSimulationTime}.")
        yield simulator.Timeout(TimeSpan.FromHours(waitFor))
        print(F"Machine {sim_process.Machine.Description} waited {simulator.CurrentSimulationTime - start} (done at {simulator.CurrentSimulationTime}).")


class PythonEnumerator():
    def __init__(self, generator, *args):
        self.generator = generator(*args)
        self.current = None

    def MoveNext(self):
        try:
            self.current = next(self.generator)
            return True
        except StopIteration:
            return False
        
    def Current(self):
        return self.current

    def Dispose(self):
        pass
```

Nun kann die Simulation gestartet werden.
```python
scenario.Run()
```

Zu beachten ist, dass beim Starten der Simulation für alle nicht konfigurierten Parameter (hier beispielsweise der Steuerungsalgorithmus) die in der Klasse ```ProductionScenario``` definierten Standardwerte verwendet werden. Diese sind in der Datei [ProductionScenario.cs](ProcessSimulator/Scenarios/ProductionScenario.cs) zu finden. Sollten andere Standardwerte gewünscht sein, können diese dort angepasst werden oder es kann ein weiteres Szenario erstellt werden, welches die gewünschten Standardwerte verwendet.

Im Anschluss können noch einige Statistiken ausgegeben werden.
```python
scenario.CollectStats()
```

### Unterschiede zwischen Python und .NET <a id="unterschiede-python-net"></a>
Die Anwendung für Python ist prinzipiell identisch mit der in C#. Alle in .NET verwendete Funktionalität ist auch in Python verwendbar. Abgesehen vom Laden der Klassenbibliotheken der C#-Klassen mit Hilfe des Moduls ```pythonnet``` gibt es aber einige Unterschiede, die etwas genauer erläutert werden sollten.

#### Verwendung von Linq-Funktionen
Die Verwendung der Linq-Funktionen (bspw. ```Where```, ```Any```, ```Skip```, ```OrderBy```, ...) ist theoretisch auch in Python möglich. Auf sie muss dabei als statische Funktionen der entsprechenden Klassen (bspw. ```Enumerable```) zugegriffen werden. Dies macht den Code allerdings länger, verschachtelter und damit schlechter les- und wartbarer.

Es sollten daher eher Python-typische Methoden verwendet werden, um mit Listen zu arbeiten (filter, sorted, dropwhile,...). Dies ist ebenso möglich. Allerdings ist dabei zu beachten, dass die entstandene Python-Liste am Ende wieder in eine C#-```List``` umgewandelt wird, da nur Objekte dieses Typs an die Simulationssoftware übergeben werden können.

Betrachten wir beispielsweise diesen C#-Code für das Filtern von Operationen, die noch nicht abgeschlossen sind:
```csharp
operationsToSimulate
    .Where(op => !op.State.Equals(OperationState.InProgress)
        && !op.State.Equals(OperationState.Completed))
    .ToList()
```

Dieser Code kann in Python wie folgt umgesetzt werden, wenn die ```filter```-Funktion verwendet wird:

```python
operations_to_plan = list(filter(
    lambda op: (not op.State.Equals(OperationState.InProgress)) and (not op.State.Equals(OperationState.Completed)),
    operations_to_simulate
))
operations_to_plan_list = List[WorkOperation]()
for op in operations_to_plan:
    operations_to_plan_list.Add(op)
```

Möglich wäre aber auch eine direkte Übersetzung in Python mit Hilfe der Linq-Funktionen:
```python
operations_to_plan = Enumerable.Where[WorkOperation](
    operations_to_simulate,
    Func[WorkOperation, bool](
        lambda op: (not op.State.Equals(OperationState.InProgress)) and (not op.State.Equals(OperationState.Completed))
    )
```

Vor allem bei mehreren aufeinanderfolgenden Linq-Funktionen ist die Verwendung der Python-Funktionen aber deutlich übersichtlicher.

Ein weiteres Beispiel aus einer (nicht mehr im Code vorhandenen) Implementierung einer Right-Shift-Funktion in Python:

Der C#-Code:
```csharp
var QueuedOperationsOnDelayedMachine = operationsToSimulate
    .Where(op => op.Machine == operation.Machine)
    .OrderBy(op => op.EarliestStart)
    .ToList();
// Skip list till you find the current delayed operation, go one further and get the successor
var successorOnMachine = QueuedOperationsOnDelayedMachine
    .SkipWhile(op => !op.Equals(operation))
    .Skip(1)
    .FirstOrDefault();
```

könnte zu folgendem Python-Code umgeschrieben werden:
```python
queued_operations_on_delayed_machine = Enumerable.OrderBy[WorkOperation, DateTime](
    Enumerable.Where[WorkOperation](
        operations_to_simulate,
        Func[WorkOperation, bool](
            lambda op: op.Machine == operation.Machine
        )
    ),
    Func[WorkOperation, DateTime](
        lambda op: op.EarliestStart
    )
 )

#Skip list till you find the current delayed operation, go one further and get the successor
successor_on_machine = Enumerable.FirstOrDefault[WorkOperation](
    Enumerable.Skip[WorkOperation](
        Enumerable.SkipWhile[WorkOperation](
            queued_operations_on_delayed_machine,
            Func[WorkOperation, bool](
                lambda op: not op.Equals(operation))
            ),
        1
    )
)
```

oder aber auch zu folgendem Python-Code:
```python
queued_operations_on_delayed_machine = filter(lambda op: op.Machine == operation.Machine, operations_to_simulate)
queued_operations_on_delayed_machine = sorted(queued_operations_on_delayed_machine, key=lambda op: op.EarliestStart)

successors = list(dropwhile(lambda op: not op.Equals(operation), queued_operations_on_delayed_machine))
successor_on_machine = successors[1] if len(successors) > 1 else None
```


#### Verwendung von Funktionen/Delegates
Python-Funktionen, die (als Delegate) an die Simulationssoftware übergeben werden sollen, müssen vorher noch mit Hilfe des entsprechenden Konstruktors in eine entsprechende C#-Funktion umgewandelt werden.

Beispielsweise wird die Steuerung durch eine Funktion eines ganz bestimmten Delegate-Typs (```HandleSimulationEvent```) implementiert. In Python muss eine Funktion daher noch in den entsprechenden Typ umgewandelt werden:

```python
def event_handler(e, planner, simulation, current_plan, operations_to_simulate, finished_operations):
    ...

controller.HandleEvent = SimulationController.HandleSimulationEvent(event_handler)
```

Allgemein müssen alle Python-Funktionen noch in den entsprechenden Typ konvertiert werden, bevor sie an eine C#-Funktion übergeben werden können. Dies kann einfach über einen Funktions-Konstruktor geschehen. Beispielsweise beim Hinzufügen einer zufälligen Unterbrechung:

```python
predicate = Func[ActiveObject[Simulation], bool](lambda process: True),
...
interruptAction = Func[ActiveObject[Simulation], ProductionScenario, IEnumerable[Event]](
    lambda simObject, scenario:
        ...
    )
```

Zu beachten ist hier auch, dass in den eckigen Klammern entsprechend die richtigen Typen angegeben werden müssen. Sie entsprechen den Generic-Typen der C#-Klasse ```Func```.

#### Verwendung von Generator-Funktionen

Generator-Funktionen gibt es sowohl in Python als auch in C#. Allerdings funktionieren sie in Python etwas anders. Um Python-Generator-Funktionen in C# als ```IEnumerable``` verwenden zu können (das wird für die zufälligen Unterbrechungen benötigt), muss eine entsprechende Klasse in Python verwendet werden, die die Funktionalität eines C#-```Enumerator``` bietet. Es ist allerdings **nicht** möglich, die Interfaces ```IEnumerator``` bzw. ```IEnumerable``` in Python direkt mit einer Klasse zu implementieren. (Dazu gibt es im nächsten Punkt noch mehr Informationen.) Daher müssen zwei weitere entsprechende Klassen in C# verwendet werden (hier ```PythonGeneratorAdapter``` und ```PythonGeneratorEnumerator```), die die Python-Generator-Funktion verwendet und die Interfaces implementiert. Diese Klasse kann dann in Python verwendet werden, um sie an die C#-Funktionen übergeben zu können, die ```IEnumerable``` erwarten.

Die ```InterruptAction``` kann dann wie folgt erstellt werden:

```python
def interrupt_action(sim_process, prod_scenario):
    if isinstance(prod_scenario.Simulator, Simulator):
        simulator = prod_scenario.Simulator
    else:
        raise Exception("Simulator is not of type Simulator")
    
    if isinstance(sim_process, MachineModel):
        waitFor = 2
        start = simulator.CurrentSimulationTime
        Log.Logger.Warning(F"Interrupted Machine {sim_process.Machine.Description} at {simulator.CurrentSimulationTime}.")
        yield simulator.Timeout(TimeSpan.FromHours(waitFor))
        print(F"Machine {sim_process.Machine.Description} waited {simulator.CurrentSimulationTime - start} (done at {simulator.CurrentSimulationTime}).")

...

interruptAction= Func[ActiveObject[Simulation], ProductionScenario, IEnumerable[Event]](
    lambda simObject, scenario: \
        PythonGeneratorAdapter[Event](PythonEnumerator(interrupt_action, simObject, scenario))
    )
```

Hier wird die Generator-Funktion ```interrupt_action```, die mit ```yield``` Events zurückgibt, an den Konstruktor der Klasse ```PythonEnumerator``` übergeben, der diese Funktion dann mit dem ebenfalls übergebenen Argument aufruft. Dieses ```PythonEnumerator```-Objekt wird dann wiederum an die C#-Klasse ```PythonGeneratorAdapter``` übergeben, die das ```IEnumerable```-Interface implementiert, was dem von der ```AddInterrupt```-Methode erwarteten Typ entspricht.

Dazu muss vorher im Python-Code die Klasse ```PythonEnumerator``` definiert werden:

```python
class PythonEnumerator():
    def __init__(self, generator, arg):
        self.generator = generator(arg)
        self.current = None

    def MoveNext(self):
        try:
            self.current = next(self.generator)
            return True
        except StopIteration:
            return False
        
    def Current(self):
        return self.current

    def Dispose(self):
        pass
```

Die verwendeten C#-Klassen sehen wie folgt aus:

```csharp
public class PythonGeneratorAdapter<T> : IEnumerable<T>
{
    private dynamic pythonEnumerator;

    public PythonGeneratorAdapter(dynamic pythonEnumerator)
    {
        this.pythonEnumerator = pythonEnumerator;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new PythonGeneratorEnumerator<T>(pythonEnumerator);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class PythonGeneratorEnumerator<T> : IEnumerator<T>
{
    private dynamic pythonEnumerator;
    public PythonGeneratorEnumerator(dynamic pythonEnumerator)
    {
        this.pythonEnumerator = pythonEnumerator;
    }

    public T Current => pythonEnumerator.Current();

    object IEnumerator.Current => pythonEnumerator.Current();

    public void Dispose()
    {
        pythonEnumerator.Dispose();
    }

    public bool MoveNext()
    {
        return pythonEnumerator.MoveNext();
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }
}
```

#### Verwendung von C#-Interfaces in Python

Bezüglich der Verwendung von C#-Interfaces in Python gibt es einige Einschränkungen. Allgemein kann von der direkten Verwendung von C#-Interfaces in Python nur abgeraten werden. Diese werden von der Bibliothek ```pythonnet``` nicht korrekt unterstützt. (Ein Pull-Request, der dieses Problem (zumindest teilweise) beheben soll, ist bereits erstellt, aber noch nicht gemerged: https://github.com/pythonnet/pythonnet/pull/2019)

Es ist **nicht** möglich, in Python ein Interface zu implementieren, das in C# definiert wurde. Python-Klassen können höchstens von C#-Klassen erben, die ein Interface implementieren. Alternativ kann die Verwendung abstrakter C#-Klassen anstelle von Interfaces in Betracht gezogen werden.

In manchen Situationen, wenn als Typ-Argument für die Generic-Typen mancher C#-Klassen ein Interface verwendet wird, kann es zu Problemen kommen. Beispielsweise in diesem Code:

```python
Func[..., IScenario, ...] (
    lambda simObject, scenario: ..
)
```
Wird die hier erstellte C#-Funktion im C#-Code mit einer Instanz eines ```ProductionScenario``` aufgerufen, dann erhält die Python-Lambda-Funktion trotzdem nur ein ```IScenario```-Objekt und hat auch nur Zugriff auf Properties und Methoden dieses Interfaces, obwohl das Objekt eigentlich ein ```ProductionScenario``` ist und somit auch andere Properties und Methoden hat. Dies kann auch durch explizites Typ-Checking mit ```isinstance``` und Casting nicht geändert werden.

Ein ähnlicher Fehler kann auftreten, wenn öffentliche Properties von C#-Klassen vom Typ eines Interfaces sind. Beispielsweise in diesem Code:

```csharp
public class ProductionScenario : IScenario
{
    public ISimulator? Simulator { get; private set; }
}
```

Wird in Python auf diese Property zugegriffen:

```python
simulator = prod_scenario.Simulator
```
dann ist ```simulator``` vom Typ ```ISimulator```, obwohl es eigentlich ein ```Simulator```-Objekt (oder auch eine Instanz einer anderen Klasse, die das ```ISimulator```-Interface implementiert) ist. Dies kann auch durch explizites Typ-Checking mit ```isinstance``` und Casting nicht geändert werden.


### Ausführen der Python-Anwendung
Das Python-Skript muss mit einem Argument ```-s``` bzw. ```--source```  aufgerufen werden, welches den **absoluten** Pfad zum Root-Ordner dieses Projektes enthält. Dieser Pfad wird benötigt, um die Klassenbibliotheken zu laden. Die entsprechenden .dll-Dateien sollten sich dann in einem Unterordner ```ProcessSimulator/bin/Debug/net6.0``` befinden.
Diese Dateien erhält man mit dem Befehl ```dotnet publish -c Debug```, ausgeführt im Ordner ```ProcessSimulator```.
