import { Link } from "react-router-dom";

export const Home = () => {
	return (
		<div className="content" style={{ paddingLeft: "15%", paddingRight: "15%" }}>
			<div className="heading">Simulationskonfigurator für das REPLAKI-Projekt</div>
			<div className="text">
				<p>
					Dieser Simulationskonfigurator ist ein Werkzeug, das im Rahmen des REPLAKI-Projekts
					entwickelt wird. Er soll die Konfiguration von{" "}
					<a href="https://github.com/eshadepunkt/SimpleProcessSim">
						Simulationen stochastischer Produktionen
					</a>{" "}
					vereinfachen.
				</p>
				<p>
					Mit Hilfe dieses Simulationskonfigurators können Sie das Modell einer Produktion
					konfigurieren, welches simuliert werden soll. Dazu legen Sie <b>Werkzeuge</b>,{" "}
					<b>Maschinen</b> und <b>Arbeitspläne</b> an, die in der Produktion verwendet werden
					sollen. Diese können Sie dann als JSON-Datei exportieren, welche Sie für die Simulation
					mit der <a href="https://github.com/eshadepunkt/SimpleProcessSim">hier</a> verfügbaren
					Simulationssoftware verwenden können. Eine ausführliche Dokumentation dieser finden Sie{" "}
					ebenfalls unter dem oben angegebenen Link.
				</p>
				<p>
					Im Folgenden finden Sie eine kurze Einführung in die Benutzung des
					Simulationskonfigurators.
				</p>

				<h3>Werkzeuge</h3>
				<p>
					Unter dem Menüpunkt <Link to={"/tools"}>Werkzeuge</Link> können Sie die Werkzeuge anlegen,
					die in der Produktion auf den verschiedenen Maschinen verwendet werden sollen. Dazu geben
					Sie einen <b>Namen</b> und eine <b>Beschreibung</b> für das Werkzeug an.
				</p>

				<h3>Maschinen</h3>
				<p>
					Unter dem Menüpunkt <Link to={"/machines"}>Maschinen</Link> können Sie die Arten von
					Maschinen anlegen, die in der Produktion verwendet werden sollen. Dazu geben Sie für jeden
					Maschinentyp einen <b>Namen</b> an, sowie die <b>Anzahl</b> der Maschinen dieses Typs, die
					in der Produktion verwendet werden sollen. Außerdem können Sie die <b>Werkzeuge</b>{" "}
					auswählen, die auf der Maschine verwendet werden können. <br />
					Zusätzlich können Sie dann angeben, wie lange das Umrüsten der Maschine auf ein anderes
					Werkzeug dauert. Diese <b>Rüstzeiten</b> werden in einer Matrix dargestellt, in der Sie
					für jede Kombination von Werkzeugen die Rüstzeit angeben können. Diese Matrix ist so
					aufgebaut, dass die Zeilen die Werkzeuge darstellen, die aktuell auf der Maschine
					verwendet werden, und die Spalten die Werkzeuge, auf die die Maschine umgerüstet werden
					soll. Die Rüstzeit wird dann in der Zelle angegeben, die sich in der Zeile des aktuellen
					Werkzeugs und in der Spalte des Werkzeugs befindet, auf das umgerüstet werden soll. Es ist
					auch möglich, bei der Umrüstzeit von einem Werkzeug auf das gleiche Werkzeug
					(Diagonalelemente der Matrix) eine Rüstzeit anzugeben, die größer als 0 ist. Dies kann zum
					Beispiel sinnvoll sein, wenn vor jedem Arbeitsgang auf dieser Maschine mit diesem Werkzeug
					eine Wartung oder Reinigung durchgeführt werden muss.
				</p>

				<h3>Arbeitspläne</h3>
				<p>
					Unter dem Menüpunkt <Link to={"/workplans"}>Arbeitspläne</Link> können Sie die
					Arbeitspläne anlegen, die in der Produktion verwendet werden sollen. Dazu geben Sie einen{" "}
					<b>Namen</b> und eine <b>Beschreibung</b> für den Arbeitsplan an.
					<br />
					Anschließend können Sie die <b>Arbeitsgänge</b> des Arbeitsplans anlegen. Dazu geben Sie
					für jeden Arbeitsgang einen <b>Namen</b> und eine <b>Dauer</b> an. Außerdem können Sie
					angeben, auf welcher <b>Maschine</b> und mit welchem <b>Werkzeug</b> der Arbeitsgang
					durchgeführt werden soll. In der Simulation werden diese Arbeitsgänge dann in der
                    angegebenen Reihenfolge durchgeführt, um das Produkt herzustellen.
				</p>
			</div>
		</div>
	);
};
