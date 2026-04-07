import { BarChart2, Activity, Shield, Wallet } from 'lucide-react';
import { useTradingHub } from '../api/useTradingHub';
import { useEffect, useState } from 'react';
import { dashboardApi } from '../api/dashboardApi';
import type { DashboardStats, PortfolioValue } from '../api/models';

export default function Dashboard() {
  const { connection, status } = useTradingHub();
  const [livePnl, setLivePnl] = useState(0);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [portfolio, setPortfolio] = useState<PortfolioValue | null>(null);
  const [loading, setLoading] = useState(true);

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(val);
  };

  useEffect(() => {
    let mounted = true;
    const fetchData = async () => {
      try {
        const [statsData, portfolioData] = await Promise.all([
          dashboardApi.getStats(),
          dashboardApi.getPortfolio()
        ]);
        if (mounted) {
          setStats(statsData);
          setPortfolio(portfolioData);
          setLoading(false);
        }
      } catch (err) {
        console.error('Failed to fetch dashboard data', err);
        if (mounted) setLoading(false);
      }
    };
    fetchData();
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    if (connection) {
      connection.on('OnStrategyUpdate', (_, __, ___, pnl: number) => {
        setLivePnl(prev => prev + pnl);
      });
    }
    return () => {
      connection?.off('OnStrategyUpdate');
    };
  }, [connection]);

  if (loading) {
    return (
      <div className="flex flex-col gap-6 animate-pulse">
        <div className="h-8 w-48 bg-primary/20 rounded"></div>
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="h-28 bg-primary/10 rounded-lg"></div>)}
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-white">Dashboard Overview</h1>
        <div className="flex items-center gap-2 text-sm">
          <span className="text-muted">Live Data</span>
          <div className={`h-2.5 w-2.5 rounded-full ${status === 'connected' ? 'bg-success shadow-[0_0_8px_theme(colors.success)]' : 'bg-danger'}`} />
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-4">
        {[
          { title: 'Total Portfolio', value: portfolio ? formatCurrency(portfolio.totalValueUsd) : '$0.00', icon: Wallet, trend: 'Net positions', color: 'text-primary' },
          { title: 'Active Strategies', value: stats?.activeStrategies.toString() || '0', icon: Activity, trend: 'Running', color: 'text-primary' },
          { title: "Session PNL", value: formatCurrency(livePnl), icon: BarChart2, trend: livePnl !== 0 ? 'Live update' : '-', color: livePnl > 0 ? 'text-success' : livePnl < 0 ? 'text-danger' : 'text-primary' },
          { title: 'System Health', value: stats?.systemHealth.state || 'UNKNOWN', icon: Shield, trend: stats?.systemHealth.details || 'Unable to fetch status', color: stats?.systemHealth.state === 'OK' ? 'text-success' : stats?.systemHealth.state === 'WARN' ? 'text-warning' : 'text-danger' },
        ].map((stat, i) => (
          <div key={i} className={`card p-6 flex flex-col gap-2 relative overflow-hidden group hover:border-primary/50 transition-colors ${stat.title === 'System Health' && stat.value !== 'OK' ? 'border-danger/30 bg-danger/5' : ''}`}>
            <div className="absolute right-0 top-0 opacity-[0.03] transform translate-x-2 -translate-y-2 group-hover:scale-110 transition-transform">
              <stat.icon size={80} />
            </div>
            <div className="flex items-center gap-2 text-muted">
              <stat.icon className="h-4 w-4" />
              <h3 className="text-sm font-medium">{stat.title}</h3>
            </div>
            <p className={`text-3xl font-bold ${stat.title === 'System Health' ? stat.color : 'text-white'}`}>{stat.value}</p>
            <p className={`text-xs font-medium ${stat.color}`}>{stat.trend}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="card col-span-2 p-6 min-h-[300px]">
          <h3 className="text-sm font-medium text-muted mb-4">Portfolio Holdings</h3>
          {portfolio?.breakdown && portfolio.breakdown.length > 0 ? (
            <div className="space-y-3">
              {portfolio.breakdown.map((item, idx) => (
                <div key={idx} className="flex justify-between items-center border-b border-white/5 pb-2">
                  <div className="flex flex-col">
                    <span className="font-semibold text-white">{item.symbol}</span>
                    <span className="text-xs text-muted">{item.quantity.toLocaleString()} units @ {formatCurrency(item.price)}</span>
                  </div>
                  <span className="font-mono text-sm">{formatCurrency(item.valueUsd)}</span>
                </div>
              ))}
            </div>
          ) : (
            <div className="h-full flex items-center justify-center text-muted text-sm pb-10">
              No active positions found
            </div>
          )}
        </div>
        <div className="card p-6 min-h-[300px]">
          <h3 className="text-sm font-medium text-muted mb-4">Recent Activity</h3>
          <div className="flex flex-col gap-4 text-sm">
            {stats?.recentOrders && stats.recentOrders.length > 0 ? (
              stats.recentOrders.map(order => (
                <div key={order.id} className="flex flex-col border-b border-border pb-2 gap-1">
                  <div className="flex justify-between items-center">
                    <span className="text-text font-medium">{order.side} {order.symbol}</span>
                    <span className={`text-xs ${order.status === 'filled' ? 'text-success' : 'text-muted'}`}>
                      {order.status.toUpperCase()}
                    </span>
                  </div>
                  <div className="flex justify-between items-center text-xs text-muted">
                    <span>{new Date(order.createdAt).toLocaleDateString()}</span>
                    {order.fillPrice && <span>@ {formatCurrency(order.fillPrice)}</span>}
                  </div>
                </div>
              ))
            ) : (
              <span className="text-muted text-center py-4">No recent activity</span>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}


/*

1 - I believe the ideal approach is to have two columns:
Status:
  - Active, inactive
Execution: (or another suitable name)
  - Waiting: Waiting for the task to start;
  - Running: Running;
  - Completed: When the task is successfully completed;
  - Cancelled: When the user interrupts the task before completion;
  - Error: When an error occurs in the task;
When "Execution" has the status "Error", the label should be red, and clicking on it should display a modal or tooltip (without any control) showing the error message received by the WebSocket.

2 - When the strategy is running, the "Execute" button should be hidden and replaced with "Stop";

3 - Confirmation modal first;

4 - Answered in the first question;

5 - A SignalR connection will receive the completion event. This event will change the status from "Running" to "Stopped" and the Profit/Loss columns will be updated;

6 - Please use only: Binance, Bitfinex, Bybit, Coinbase Advanced, Kraken, KuCoin and OKX;

7 - Yes


*/