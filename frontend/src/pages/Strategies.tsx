import { useState, useEffect } from 'react';
import Editor from '@monaco-editor/react';
import { Play, Save, Activity, Plus } from 'lucide-react';
import { strategiesApi } from '../api/strategiesApi';
import type { StrategyDto } from '../api/models';

export default function Strategies() {
  const [strategies, setStrategies] = useState<StrategyDto[]>([]);
  const [selectedStrategy, setSelectedStrategy] = useState<StrategyDto | null>(null);
  const [script, setScript] = useState<string>('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [running, setRunning] = useState(false);

  useEffect(() => {
    fetchStrategies();
  }, []);

  const fetchStrategies = async () => {
    try {
      const data = await strategiesApi.getAll();
      setStrategies(data);
      if (data.length > 0 && !selectedStrategy) {
        setSelectedStrategy(data[0]);
        setScript(data[0].script);
      }
    } catch (err) {
      console.error('Failed to fetch strategies', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (strat: StrategyDto) => {
    setSelectedStrategy(strat);
    setScript(strat.script);
  };

  const handleSave = async () => {
    if (!selectedStrategy) return;
    setSaving(true);
    try {
      await strategiesApi.update(selectedStrategy.id, {
        script,
        parameters: selectedStrategy.parameters
      });
      fetchStrategies(); // refresh list
    } catch (err) {
      console.error('Failed to save strategy', err);
    } finally {
      setSaving(false);
    }
  };

  const handleDeploy = async () => {
    if (!selectedStrategy) return;
    setRunning(true);
    try {
      await strategiesApi.start(selectedStrategy.id);
      fetchStrategies();
    } catch (err) {
      console.error('Failed to start strategy', err);
    } finally {
      setRunning(false);
    }
  };

  if (loading) {
    return <div className="animate-pulse h-full bg-primary/10 rounded-xl"></div>;
  }

  return (
    <div className="flex h-full flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight text-white">Strategy Editor</h1>
        {selectedStrategy && (
          <div className="flex gap-2">
            <button 
              onClick={handleSave} 
              disabled={saving}
              className="btn btn-outline gap-2 text-muted"
            >
               <Save className="h-4 w-4" /> {saving ? 'Saving...' : 'Save Draft'}
            </button>
            <button className="btn btn-outline gap-2 text-primary border-primary/20 hover:bg-primary/10">
               <Activity className="h-4 w-4" /> Run Backtest
            </button>
            <button 
              onClick={handleDeploy}
              disabled={running || selectedStrategy.status === 'running'}
              className="btn btn-primary gap-2"
            >
               <Play className="h-4 w-4 fill-current" /> 
               {selectedStrategy.status === 'running' ? 'Running' : running ? 'Deploying...' : 'Deploy Live'}
            </button>
          </div>
        )}
      </div>

      <div className="flex flex-col lg:flex-row gap-6 flex-1 min-h-0">
        <div className="w-full lg:w-64 card flex flex-col overflow-hidden">
          <div className="p-4 border-b border-border flex justify-between items-center">
             <h3 className="font-semibold text-white">My Strategies</h3>
             <button className="btn p-1 text-primary hover:bg-primary/10 rounded">
                <Plus className="h-4 w-4" />
             </button>
          </div>
          <div className="flex-1 overflow-y-auto p-2 space-y-1">
            {strategies.map(s => (
              <button 
                key={s.id}
                onClick={() => handleSelect(s)}
                className={`w-full text-left p-3 rounded-lg flex flex-col gap-1 transition-colors ${selectedStrategy?.id === s.id ? 'bg-primary/10 border border-primary/20' : 'hover:bg-white/5 border border-transparent'}`}
              >
                <span className="text-sm font-medium text-white">{s.name}</span>
                <span className={`text-xs ${s.status === 'running' ? 'text-success' : 'text-muted'}`}>
                  {s.status.toUpperCase()} • {s.symbol}
                </span>
              </button>
            ))}
            {strategies.length === 0 && (
              <div className="p-4 text-center text-xs text-muted">No strategies created.</div>
            )}
          </div>
        </div>

        {selectedStrategy ? (
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6 flex-1 min-h-0">
            <div className="card xl:col-span-2 flex flex-col overflow-hidden">
               <div className="bg-surfaceHighlight px-4 py-2 border-b border-border text-sm font-medium flex justify-between items-center">
                 <span className="text-muted">{selectedStrategy.name} (Python)</span>
                 <span className={`text-xs ${selectedStrategy.status === 'running' ? 'text-success bg-success/10 px-2 py-0.5 rounded' : 'text-muted'}`}>
                   {selectedStrategy.status.toUpperCase()}
                 </span>
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
                     <div className="flex justify-between items-center">
                       <span className="text-sm text-muted">Symbol</span>
                       <span className="text-sm font-medium text-white">{selectedStrategy.symbol}</span>
                     </div>
                     <div className="flex justify-between items-center">
                       <span className="text-sm text-muted">Timeframe</span>
                       <span className="text-sm font-medium text-white">{selectedStrategy.timeframe}</span>
                     </div>
                     <div className="flex justify-between items-center">
                       <span className="text-sm text-muted">Mode</span>
                       <span className="text-sm font-medium text-white uppercase">{selectedStrategy.mode}</span>
                     </div>
                  </div>
               </div>

               <div className="card p-4 flex-1">
                  <h3 className="font-semibold text-white mb-4">Activity Logs</h3>
                  <div className="font-mono text-xs text-muted bg-[#0c0c0e] p-3 rounded border border-white/5 h-full overflow-y-auto">
                     <p className="text-muted italic">Logs stream will appear here...</p>
                     {selectedStrategy.circuitFailures > 0 && (
                       <p className="text-danger mt-2">[{new Date().toLocaleTimeString()}] Circuit breaker failures: {selectedStrategy.circuitFailures}</p>
                     )}
                  </div>
               </div>
            </div>
          </div>
        ) : (
          <div className="card flex-1 flex flex-col items-center justify-center text-center p-12">
            <Activity className="h-12 w-12 text-muted mb-4 opacity-50" />
            <h3 className="text-lg font-medium text-white mb-2">No Strategy Selected</h3>
            <p className="text-muted text-sm max-w-sm">Select a strategy from the sidebar or create a new one to start editing.</p>
          </div>
        )}
      </div>
    </div>
  );
}
