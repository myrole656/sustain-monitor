<?php

namespace App\Filament\User\Widgets;

use App\Models\Project;
use App\Models\Process;
use Filament\Widgets\StatsOverviewWidget as BaseWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
use Illuminate\Support\Facades\Auth;

class UserProjectStats extends BaseWidget
{
    protected ?string $heading = 'Project Statistics';

    public ?int $projectId = null;

    protected $listeners = ['projectSelected' => 'updateProject'];

    public function updateProject($projectId)
    {
        $this->projectId = $projectId;
        $this->dispatch('$refresh');
    }

    public function getHeading(): string
    {
        if ($this->projectId) {
            $p = Project::find($this->projectId);
            return $p?->project_name ?? 'Project Statistics';
        }

        // No project selected, show default heading
        return 'Project Statistics';
    }

    protected function getStats(): array
    {
        $user = Auth::user();

        // Only get project if projectId is set
        $project = $this->projectId
            ? Project::when($user->role !== 'admin', fn($q) => $q->where('user_id', $user->id))
                     ->find($this->projectId)
            : null;

        $projectId = $project?->id ?? null;

        // Get process for this project if project exists
        $process = $projectId ? Process::where('project_id', $projectId)->first() : null;

        // Compute completion
        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 31,
            'Execution'  => 60,
            'Monitoring' => 12,
            'Closing'    => 14,
        ];

        $stepMarks = $process ? [
            'Initiation' => $process->initiation,
            'Planning'   => $process->planning,
            'Execution'  => $process->execution,
            'Monitoring' => $process->monitoring,
            'Closing'    => $process->closing,
        ] : array_fill_keys(array_keys($maxMarks), 0);

        $totalMax = array_sum($maxMarks);
        $totalUserMark = array_sum($stepMarks);
        $overallPercentage = $totalMax ? round(($totalUserMark / $totalMax) * 100, 1) : 0;

        // -----------------------
        // Stats array
        // -----------------------
        $stats = [];

        // STAT 1
           if ($user->role === 'admin') {
    $stats[] = Stat::make(
        $project?->project_name ?? 'N/A',
        $project?->project_location ?? 'N/A'
    )
    ->description($project?->pic_contact ?? 'N/A');
} else {
    $totalProjects = Project::where('user_id', $user->id)->count();

    $stats[] = Stat::make('Total Projects', $totalProjects)
        ->description('All projects you created')
        ->descriptionIcon('heroicon-o-folder');
}



        // STAT 2: Target
        $stats[] = Stat::make('Target', $project?->target ?? 'N/A')
            ->description('Target of selected project')
            ->descriptionIcon('heroicon-o-flag')
            ->color(match($project?->target) {
                'PLATINUM' => 'success',
                'GOLD'     => 'warning',
                'SILVER'   => 'danger',
                'CERTIFIED'=> 'primary',
                default    => 'secondary',
            });

        // STAT 3: Completion
        $stats[] = Stat::make('Project Completion', $project ? $overallPercentage . '%' : 'N/A')
            ->description('Progress based on process marks')
            ->descriptionIcon('heroicon-o-chart-pie')
            ->color($overallPercentage >= 80 ? 'success' :
                   ($overallPercentage >= 50 ? 'warning' : 'danger'));

        // STAT 4: Status
        $status = $process?->status ?? $project?->status ?? 'N/A';
        $stats[] = Stat::make('Status', $status)
            ->description('Final status of selected project')
            ->descriptionIcon('heroicon-o-check-circle')
            ->color(match($status) {
                'PLATINUM'  => 'success',
                'GOLD'      => 'warning',
                'SILVER'    => 'primary',
                'CERTIFIED' => 'success',
                'FAIL'      => 'danger',
                default     => 'secondary',
            });

        return $stats;
    }
}
