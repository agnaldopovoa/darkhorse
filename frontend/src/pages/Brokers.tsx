import { Key, Plus, Trash2 } from 'lucide-react';

export default function Brokers() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-white">Broker Credentials</h1>
        <button className="btn btn-primary gap-2">
          <Plus className="h-4 w-4" />
          Add Broker
        </button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {/* Mock Data */}
        <div className="card p-6 group hover:border-primary/50 transition-all cursor-pointer">
          <div className="flex justify-between items-start mb-4">
             <div className="flex items-center gap-3">
               <div className="p-2 rounded-md bg-primary/10 text-primary">
                 <Key className="h-5 w-5" />
               </div>
               <div>
                  <h3 className="font-semibold text-white">Binance Main</h3>
                  <p className="text-xs text-muted">Status: <span className="text-success">Active</span></p>
               </div>
             </div>
             <button className="text-muted hover:text-danger opacity-0 group-hover:opacity-100 transition-opacity">
                <Trash2 className="h-4 w-4" />
             </button>
          </div>
          <div className="text-xs text-muted mt-4 border-t border-border pt-4">
            <div className="flex justify-between"><span>Fee Rate:</span> <span className="text-text">0.1%</span></div>
            <div className="flex justify-between mt-1"><span>Mode:</span> <span className="text-primary font-medium">Live</span></div>
          </div>
        </div>
      </div>
    </div>
  );
}
