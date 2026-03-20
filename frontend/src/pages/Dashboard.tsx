import { BarChart2, Activity, Shield, Wallet } from 'lucide-react';
import { useTradingHub } from '../api/useTradingHub';
import { useEffect, useState } from 'react';

export default function Dashboard() {
  const { connection, status } = useTradingHub();
  const [livePnl, setLivePnl] = useState(0);

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
          { title: 'Total Portfolio', value: '$24,562.00', icon: Wallet, trend: '+4.2%' },
          { title: 'Active Strategies', value: '3', icon: Activity, trend: 'All running' },
          { title: "Today's PNL", value: `$${livePnl.toFixed(2)}`, icon: BarChart2, trend: livePnl > 0 ? '+ Live' : 'Live', color: livePnl >= 0 ? 'text-success' : 'text-danger' },
          { title: 'System Health', value: '100%', icon: Shield, trend: 'No circuit breaks' },
        ].map((stat, i) => (
          <div key={i} className="card p-6 flex flex-col gap-2 relative overflow-hidden group hover:border-primary/50 transition-colors">
            <div className="absolute right-0 top-0 opacity-[0.03] transform translate-x-2 -translate-y-2 group-hover:scale-110 transition-transform">
               <stat.icon size={80} />
            </div>
            <div className="flex items-center gap-2 text-muted">
              <stat.icon className="h-4 w-4" />
              <h3 className="text-sm font-medium">{stat.title}</h3>
            </div>
            <p className="text-3xl font-bold text-white">{stat.value}</p>
            <p className={`text-xs font-medium ${stat.color || 'text-primary'}`}>{stat.trend}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="card col-span-2 p-6 flex flex-col justify-center items-center min-h-[300px]">
           <p className="text-muted text-sm">Portfolio chart implementation goes here (Recharts)</p>
        </div>
        <div className="card p-6 min-h-[300px]">
           <h3 className="text-sm font-medium text-muted mb-4">Recent Activity</h3>
           <div className="flex flex-col gap-4 text-sm">
              <div className="flex justify-between items-center border-b border-border pb-2">
                 <span className="text-text">BUY BTC/USDT</span>
                 <span className="text-success text-xs">Filled @ $64,200</span>
              </div>
           </div>
        </div>
      </div>
    </div>
  );
}
