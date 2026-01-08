import React from 'react'
import { Navigate } from 'react-router-dom'
import usePermissions from '../../shared/hooks/usePermissions'
import { PermissionRole } from '../../types'

interface ProtectedRouteProps {
  requiredRole?: PermissionRole;
  children: React.ReactNode;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  requiredRole = PermissionRole.READONLY,
  children,
}) => {
  const { hasRole } = usePermissions()
  if (!hasRole(requiredRole)) {
    return <Navigate to="/" replace />
  }
  return <>{children}</>
}
