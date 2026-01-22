/**
 * ProtectedRoute - Wrapper for authenticated routes
 *
 * Redirects to login if user is not authenticated.
 * Optionally checks for required role.
 *
 * @module core/components
 */
import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Loading } from '../../shared/components/Loading';

interface ProtectedRouteProps {
  /** Required role to access this route (optional) */
  requiredRole?: string;
  /** Content to render if authorized */
  children: React.ReactNode;
}

/**
 * ProtectedRoute component
 * Shows loading state during auth check, redirects to login if unauthenticated
 */
export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  requiredRole,
  children,
}) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  // Show loading state during initial auth check
  if (isLoading) {
    return <Loading />;
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // Check role if required (Admin can access everything)
  if (requiredRole && user?.role !== requiredRole && user?.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
};
