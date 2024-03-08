import { Routes, Route, NavLink } from "react-router-dom";
import "./App.css";
import { Tools } from "./components/Tools";
import { Home } from "./components/Home";
import { Machines } from "./components/Machines";
import { WorkPlans } from "./components/WorkPlans";
import { ImportExport } from "./components/ImportExport";
import 'bootstrap/dist/css/bootstrap.min.css';

function App() {
	return (
		<div className="App">
			<nav>
				<NavLink to="/">Startseite</NavLink>
				<NavLink to="/json">Import/Export</NavLink>
				<NavLink to="/tools">Werkzeuge</NavLink>
				<NavLink to="/machines">Maschinen</NavLink>
				<NavLink to="/workplans">Arbeitspläne</NavLink>
			</nav>
			<Routes>
				<Route path="/" element={<Home />} />
				<Route path="/json" element={<ImportExport />} />
				<Route path="/tools" element={<Tools />} />
				<Route path="/machines" element={<Machines />} />
				<Route path="/workplans" element={<WorkPlans />} />
			</Routes>
		</div>
	);
}

export default App;

