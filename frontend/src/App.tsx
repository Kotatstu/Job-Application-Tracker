import { Routes, Route } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { ApplicationsListPage } from './pages/ApplicationsListPage'
import { ApplicationDetailPage } from './pages/ApplicationDetailPage'
import { CompanyDetailPage } from './pages/CompanyDetailPage'
import { RegisterPage } from './pages/RegisterPage'
import { Layout } from './components/Layout'
import { CreateApplicationPage } from './pages/CreateApplicationPage'
import { EditApplicationPage } from './pages/EditApplicationPage'
import { MailboxPage } from './pages/MailboxPage'
import { ReviewPage } from './pages/ReviewPage'

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route path="/" element={<ApplicationsListPage />} />
          <Route path="/applications/:id" element={<ApplicationDetailPage />} />
          <Route path="/companies/:id" element={<CompanyDetailPage />} />
          <Route path="/applications/new" element={<CreateApplicationPage />} />
          <Route path="/applications/:id/edit" element={<EditApplicationPage />} />
          <Route path="/mailbox" element={<MailboxPage />} />          
          <Route path="/review" element={<ReviewPage />} />
        </Route>
      </Route>
    </Routes>
  )
}

export default App