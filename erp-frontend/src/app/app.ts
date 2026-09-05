import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="app-layout" *ngIf="authService.isLoggedIn(); else loginLayout">
      <!-- Sidebar Navigation -->
      <nav class="sidebar">
        <div class="sidebar-header">
          <h2>ERP AI Enterprise 🚀</h2>
          <span class="system-badge">v2.4 Online</span>
        </div>
        
        <div class="nav-links">
          <a routerLink="/dashboard" routerLinkActive="active">
            <span class="icon">📊</span> Dashboard
          </a>
          <a routerLink="/products" routerLinkActive="active">
            <span class="icon">📦</span> Products Catalog
          </a>
          <a routerLink="/inventory" routerLinkActive="active">
            <span class="icon">🏭</span> Inventory & Stock
          </a>
          <a routerLink="/suppliers" routerLinkActive="active">
            <span class="icon">🤝</span> Suppliers CRM
          </a>
          <a routerLink="/purchase-orders" routerLinkActive="active">
            <span class="icon">📋</span> Purchase Orders
          </a>
          <a routerLink="/ai-assistant" routerLinkActive="active">
            <span class="icon">🤖</span> AI Assistant Copilot
          </a>
        </div>

        <div class="user-footer">
          <div class="user-info">
            <div class="avatar">{{ authService.getUsername()?.charAt(0)?.toUpperCase() || 'U' }}</div>
            <div class="user-details">
              <span class="username">{{ authService.getUsername() }}</span>
              <span class="role-pill">{{ authService.getRole() }}</span>
            </div>
          </div>
          <button (click)="logout()" class="btn-logout" title="Sign out">
            <span>🚪</span> Logout
          </button>
        </div>
      </nav>

      <!-- Main Content Area -->
      <div class="main-wrapper">
        <header class="topbar">
          <div class="topbar-left">
            <h1>Enterprise Resource Planning & Supply Chain</h1>
          </div>
          <div class="topbar-right">
            <div class="status-indicator">
              <span class="pulse-dot"></span> System Operational
            </div>
            <div class="current-date">{{ currentDate | date:'mediumDate' }}</div>
          </div>
        </header>

        <main class="content-scroll">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>

    <ng-template #loginLayout>
      <router-outlet></router-outlet>
    </ng-template>
  `,
  styles: [`
    .app-layout {
      display: flex;
      height: 100vh;
      width: 100vw;
      overflow: hidden;
      background: #f8fafc;
      font-family: 'Inter', sans-serif;
    }

    /* Sidebar Styling */
    .sidebar {
      width: 280px;
      background: linear-gradient(180deg, #0f172a 0%, #1e293b 100%);
      color: #cbd5e1;
      display: flex;
      flex-direction: column;
      box-shadow: 4px 0 15px rgba(0, 0, 0, 0.05);
      z-index: 10;
    }

    .sidebar-header {
      padding: 1.75rem 1.5rem 1.25rem 1.5rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.08);
    }

    .sidebar-header h2 {
      font-size: 1.15rem;
      font-weight: 700;
      color: #ffffff;
      letter-spacing: -0.025em;
      margin-bottom: 0.35rem;
    }

    .system-badge {
      font-size: 0.7rem;
      background: rgba(56, 189, 248, 0.15);
      color: #38bdf8;
      padding: 0.15rem 0.5rem;
      border-radius: 9999px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .nav-links {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      padding: 1.25rem 1rem;
      flex: 1;
      overflow-y: auto;
    }

    .nav-links a {
      display: flex;
      align-items: center;
      gap: 0.85rem;
      color: #94a3b8;
      text-decoration: none;
      padding: 0.85rem 1rem;
      border-radius: 8px;
      font-weight: 500;
      font-size: 0.925rem;
      transition: all 0.2s ease;
    }

    .nav-links a .icon {
      font-size: 1.1rem;
    }

    .nav-links a:hover {
      background: rgba(255, 255, 255, 0.05);
      color: #f8fafc;
      transform: translateX(2px);
    }

    .nav-links a.active {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
      color: #ffffff;
      box-shadow: 0 4px 12px rgba(59, 130, 246, 0.35);
      font-weight: 600;
    }

    /* User Footer */
    .user-footer {
      padding: 1.25rem;
      border-top: 1px solid rgba(255, 255, 255, 0.08);
      background: rgba(15, 23, 42, 0.5);
    }

    .user-info {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1rem;
    }

    .avatar {
      width: 38px;
      height: 38px;
      background: linear-gradient(135deg, #6366f1 0%, #4f46e5 100%);
      color: white;
      border-radius: 50%;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 0.95rem;
      box-shadow: 0 2px 8px rgba(99, 102, 241, 0.4);
    }

    .user-details {
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .username {
      color: #f1f5f9;
      font-weight: 600;
      font-size: 0.9rem;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .role-pill {
      font-size: 0.75rem;
      color: #cbd5e1;
      background: rgba(255, 255, 255, 0.1);
      padding: 0.1rem 0.4rem;
      border-radius: 4px;
      width: max-content;
      margin-top: 0.15rem;
    }

    .btn-logout {
      width: 100%;
      padding: 0.65rem;
      background: rgba(239, 68, 68, 0.1);
      color: #f87171;
      border: 1px solid rgba(239, 68, 68, 0.2);
      border-radius: 6px;
      font-weight: 600;
      font-size: 0.85rem;
      cursor: pointer;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      transition: all 0.2s ease;
    }

    .btn-logout:hover {
      background: #ef4444;
      color: white;
      border-color: #ef4444;
      box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3);
    }

    /* Main Wrapper */
    .main-wrapper {
      flex: 1;
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
      background: #f8fafc;
    }

    .topbar {
      height: 70px;
      background: #ffffff;
      border-bottom: 1px solid #e2e8f0;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 2rem;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.02);
      z-index: 5;
    }

    .topbar-left h1 {
      font-size: 1.1rem;
      font-weight: 600;
      color: #334155;
      letter-spacing: -0.015em;
    }

    .topbar-right {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }

    .status-indicator {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.85rem;
      font-weight: 500;
      color: #059669;
      background: #ecfdf5;
      padding: 0.35rem 0.85rem;
      border-radius: 9999px;
      border: 1px solid #a7f3d0;
    }

    .pulse-dot {
      width: 8px;
      height: 8px;
      background: #10b981;
      border-radius: 50%;
      box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
      animation: pulse 2s infinite;
    }

    @keyframes pulse {
      0% {
        transform: scale(0.95);
        box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7);
      }
      70% {
        transform: scale(1);
        box-shadow: 0 0 0 6px rgba(16, 185, 129, 0);
      }
      100% {
        transform: scale(0.95);
        box-shadow: 0 0 0 0 rgba(16, 185, 129, 0);
      }
    }

    .current-date {
      font-size: 0.875rem;
      color: #64748b;
      font-weight: 500;
    }

    .content-scroll {
      flex: 1;
      overflow-y: auto;
      background: #f8fafc;
    }
  `]
})
export class App {
  currentDate = new Date();
  constructor(public authService: AuthService, private router: Router) {}

  logout() {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
