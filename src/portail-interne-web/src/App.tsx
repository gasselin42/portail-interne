import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"
import { RequireAuth } from "./components/auth/RequireAuth"
import { RequireAdmin } from "./components/auth/RequireAdmin"
import { RequireApprover } from "./components/auth/RequireApprover"
import { SessionProvider } from "./session/SessionProvider"

import { NotFoundPage } from "./pages/NotFoundPage"
import { ErrorBoundary } from "./components/errors/ErrorBoundary"
import { AppCrash } from "./components/errors/AppCrash"

import { AppLayout } from "./layout/AppLayout"
import { LoginPage } from "./pages/LoginPage"
import { ChangePassword } from "./pages/ChangePassword"
import { HomePage } from "./pages/HomePage"
import { AdminAccountsPage } from "./pages/AdminAccountsPage"
import { CreateEmployeePage } from "./pages/CreateEmployeePage"
import { AnnuairePage } from "./pages/AnnuairePage"
import { FicheEmployePage } from "./pages/FicheEmployePage"
import { LeavesPage } from "./pages/LeavesPage"
import { ApprovalsPage } from "./pages/ApprovalsPage"
import { CalendarPage } from "./pages/CalendarPage"
import { EmployeeSchedulePage } from "./pages/EmployeeSchedulePage"
import { RequireTeamView } from "./components/auth/RequireTeamView"
import { TeamPage } from "./pages/TeamPage"

export default function App() {
	return (
		<BrowserRouter>
			<ErrorBoundary fallback={() => <AppCrash />}>
				<SessionProvider>
					<Routes>
						{/* Public */}
						<Route path="/login" element={<LoginPage />} />

						{/* Connecté + autorisé à changer le mdp */}
						<Route element={<RequireAuth allowPasswordChange />}>
							<Route element={<AppLayout />}>
								<Route path="/change-password" element={<ChangePassword />} />
							</Route>
						</Route>

						{/* Connecté + mdp déjà OK */}
						<Route element={<RequireAuth />}>
							<Route element={<AppLayout />}>
								<Route path="/" element={<HomePage />} />
								<Route path="/employees" element={<AnnuairePage />} />
								<Route path="/employees/:id" element={<FicheEmployePage />} />
								<Route path="/employees/:id/schedule" element={<EmployeeSchedulePage />} />
								<Route path="/leaves" element={<LeavesPage />} />
								<Route path="/leaves/new" element={<Navigate to="/leaves?nouvelle=1" replace />} />
								<Route path="/calendar" element={<CalendarPage />} />

								<Route element={<RequireApprover />}>
									<Route path="/approvals" element={<ApprovalsPage />} />
								</Route>

								<Route element={<RequireAdmin />}>
									<Route path="/admin/accounts" element={<AdminAccountsPage />} />
									<Route path="/admin/employees/new" element={<CreateEmployeePage />} />
									<Route path="/admin/employees/:id/edit" element={<CreateEmployeePage />} />
								</Route>

								<Route element={<RequireTeamView />}>
									<Route path="/team" element={<TeamPage />} />
								</Route>

								<Route path="*" element={<NotFoundPage />} />
							</Route>
						</Route>
					</Routes>
				</SessionProvider>
			</ErrorBoundary>
		</BrowserRouter>
	)
}
