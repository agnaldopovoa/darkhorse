import { useState } from 'react';
import Editor from '@monaco-editor/react';
import { Play, Square, Save, Activity } from 'lucide-react';

export default function Strategies() {
  const [script, setScript] = useState<string>('def init(context):\n    pass\n\ndef tick(context, data):\n    signal = "HOLD"\n    if data.close > data.open:\n        signal = "BUY"\n    return signal\n');

  return (
    <div className="flex h-full flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-white">Strategy Editor</h1>
        <div className="flex gap-2">
          <button className="btn btn-outline gap-2 text-muted">
             <Save className="h-4 w-4" /> Save Draft
          </button>
          <button className="btn btn-outline gap-2 text-primary border-primary/20 hover:bg-primary/10">
             <Activity className="h-4 w-4" /> Run Backtest
          </button>
          <button className="btn btn-primary gap-2">
             <Play className="h-4 w-4 fill-current" /> Deploy Live
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 flex-1 min-h-0">
        <div className="card lg:col-span-2 flex flex-col overflow-hidden">
           <div className="bg-surfaceHighlight px-4 py-2 border-b border-border text-sm font-medium flex justify-between items-center">
             <span className="text-muted">strategy.py (Python)</span>
           </div>
           <div className="flex-1 min-h-[400px]">
             <Editor 
               height="100%" 
               language="python" 
               theme="vs-dark" 
               value={script} 
               onChange={(val) => setScript(val || '')}
               options={{
                 minimap: { enabled: false },
                 fontSize: 14,
                 fontFamily: "'Fira Code', monospace",
                 padding: { top: 16 }
               }}
             />
           </div>
        </div>

        <div className="flex flex-col gap-6">
           <div className="card p-4">
              <h3 className="font-semibold text-white mb-4">Parameters</h3>
              <div className="flex flex-col gap-3">
                 <div className="flex flex-col gap-1.5">
                   <label className="text-xs text-muted">Trading Pair</label>
                   <select className="input h-9 text-text bg-background appearance-none">
                      <option>BTC/USDT</option>
                      <option>ETH/USDT</option>
                   </select>
                 </div>
                 <div className="flex flex-col gap-1.5">
                   <label className="text-xs text-muted">Timeframe</label>
                   <select className="input h-9 text-text">
                      <option>1m</option>
                      <option>5m</option>
                      <option>1h</option>
                   </select>
                 </div>
              </div>
           </div>

           <div className="card p-4 flex-1">
              <h3 className="font-semibold text-white mb-4">Execution Logs</h3>
              <div className="font-mono text-xs text-muted bg-[#0c0c0e] p-3 rounded border border-white/5 h-full overflow-y-auto">
                 <p className="text-success">[10:45:01] Compiled successfully.</p>
                 <p>[10:45:02] Awaiting tick data...</p>
                 <p className="text-warning">[10:46:00] Signal generated: HOLD</p>
              </div>
           </div>
        </div>
      </div>
    </div>
  );
}
