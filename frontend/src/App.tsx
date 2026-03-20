import { Routes, Route, Navigate, Link } from 'react-router-dom';
import { useAuthStore } from './store/auth';

// Pages
import AuthPage from './pages/AuthPage';
import Dashboard from './pages/Dashboard';
import Brokers from './pages/Brokers';
import Strategies from './pages/Strategies';

// Protected Route Wrapper
const ProtectedRoute = ({ children }: { children: JSX.Element }) => {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return children;
};

// Main Layout Wrapper
const MainLayout = ({ children }: { children: React.ReactNode }) => {
  return (
    <div className="flex h-screen w-full bg-background text-text font-sans">
      {/* Sidebar Placeholder */}
      <aside className="w-64 border-r border-border bg-surface p-4 hidden md:flex flex-col">
        <div className="text-xl font-bold tracking-tighter text-text mb-8 mt-2 px-3 text-white">DARKHORSE</div>
        <nav className="flex flex-col gap-1">
           <Link to="/" className="px-3 py-2 rounded hover:bg-surfaceHighlight text-muted hover:text-white transition-colors">Dashboard</Link>
           <Link to="/brokers" className="px-3 py-2 rounded hover:bg-surfaceHighlight text-muted hover:text-white transition-colors">Brokers</Link>
           <Link to="/strategies" className="px-3 py-2 rounded hover:bg-surfaceHighlight text-muted hover:text-white transition-colors">Strategies</Link>
        </nav>
      </aside>

      {/* Main Content Area */}
      <main className="flex-1 overflow-auto bg-background p-6">
        {children}
      </main>
    </div>
  );
};

function App() {
  return (
    <Routes>
      <Route path="/login" element={<AuthPage />} />
      <Route path="/register" element={<AuthPage />} />
      
      <Route path="/" element={
        <ProtectedRoute>
          <MainLayout>
             <Dashboard />
          </MainLayout>
        </ProtectedRoute>
      } />

      <Route path="/brokers" element={
        <ProtectedRoute>
          <MainLayout>
             <Brokers />
          </MainLayout>
        </ProtectedRoute>
      } />

      <Route path="/strategies" element={
        <ProtectedRoute>
          <MainLayout>
             <Strategies />
          </MainLayout>
        </ProtectedRoute>
      } />
      
      {/* Catch-all */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
