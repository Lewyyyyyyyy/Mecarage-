export interface AdminKpis {
  totalTenants: number;
  totalUsers: number;
  totalGarages: number;
  totalVehicles: number;
  totalInterventions: number;
  pendingInterventions: number;
  completedInterventions: number;
  activeAdmins: number;
  activeMechanics: number;
  activeClients: number;
}

export interface PublicTrustKpis {
  totalTenants: number;
  totalGarages: number;
  totalInterventions: number;
  completedInterventions: number;
  activeClients: number;
  successRate: number;
}

