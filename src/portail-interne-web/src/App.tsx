import { BrowserRouter, Route, Routes } from "react-router-dom";
import { RequireAuth } from "./components/RequireAuth";
import { RequireAdmin } from "./components/RequireAdmin";

import { LoginPage } from "./pages/LoginPage";
import { ChangePassword } from "./pages/ChangePassword";
import { HomePage } from "./pages/HomePage";
import { AdminAccountsPage } from "./pages/AdminAccountsPage";
import { CreateEmployeePage } from "./pages/CreateEmployeePage";
import { AnnuairePage } from "./pages/AnnuairePage";
import { FicheEmployePage } from "./pages/FicheEmployePage";
import { LeavesPage } from "./pages/LeavesPage";
import { NewLeavePage } from "./pages/NewLeavePage";

export default function App() {
	return (
		<BrowserRouter>
			<Routes>
				{/* Public */}
				<Route path="/login" element={<LoginPage />} />
				
				{/* Connecté + autorisé à changer le mdp */}
				<Route element={<RequireAuth allowPasswordChange />}>
					<Route path="/change-password" element={<ChangePassword />}/>
				</Route>

				{/* Connecté + mdp déjà OK */}
				<Route element={<RequireAuth />}>
					<Route path="/" element={<HomePage />} />
					<Route path="/employees" element={<AnnuairePage />} />
					<Route path="/employees/:id" element={<FicheEmployePage />} />
					<Route path="/leaves" element={<LeavesPage />} />
					<Route path="/leaves/new" element={<NewLeavePage />} />
					<Route path="/leaves/pending" element={<h1>À approuver</h1>} />
					<Route path="/calendar" element={<h1>Calendrier</h1>} />

					<Route element={<RequireAdmin />}>
						<Route path="/admin/accounts" element={<AdminAccountsPage />} />
						<Route path="/admin/employees/new" element={<CreateEmployeePage />} />
						<Route path="/admin/employees/:id/edit" element={<CreateEmployeePage />} />
					</Route>
					{/* plus tard: /employees, /admin/... */}
				</Route>
			</Routes>
		</BrowserRouter>
	)
}
