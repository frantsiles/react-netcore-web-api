import { useQuery } from '@tanstack/react-query';
import { userService } from '../services/userService';
import { useAuth } from '../contexts/AuthContext';

export function UsersPage() {
  const { user, logout } = useAuth();
  const { data: users, isLoading, error } = useQuery({
    queryKey: ['users'],
    queryFn: userService.getAll,
  });

  return (
    <div className="users-page">
      <header>
        <h1>Users</h1>
        <span>Logged in as <strong>{user?.fullName}</strong></span>
        <button onClick={logout}>Log out</button>
      </header>

      {isLoading && <p>Loading…</p>}
      {error && <p className="error">Failed to load users.</p>}

      {users && (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Roles</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>{u.firstName} {u.lastName}</td>
                <td>{u.email}</td>
                <td>{u.roles.join(', ')}</td>
                <td>{u.isActive ? 'Active' : 'Inactive'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
