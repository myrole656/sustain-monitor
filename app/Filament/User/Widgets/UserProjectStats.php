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

        $project = Project::where('user_id', Auth::id())->latest()->first();

        return $project?->project_name ?? 'Project Statistics';
    }

    protected function getStats(): array
    {
        $userId = Auth::id();

        // -------------------------
        // 1. Count Total Projects
        // -------------------------
        $totalProjects = Project::where('user_id', $userId)->count();

        // -------------------------
        // 2. Get selected project OR latest project
        // -------------------------
        $project = $this->projectId
            ? Project::find($this->projectId)
            : Project::where('user_id', $userId)->latest()->first();

        $projectId = $project?->id ?? null;

        // -------------------------
        // 3. Get first process for the selected project
        // -------------------------
        $process = $projectId
            ? Process::where('project_id', $projectId)->first()
            : null;

        // -------------------------
        // 4. Target (PLATINUM/GOLD/etc) from Project
        // -------------------------
        $latestTarget = $project?->target ?? 'N/A';

        // -------------------------
        // 5. Status (from Process if exists, else from Project)
        // -------------------------
        $status = $process?->status ?? $project?->status ?? 'N/A';

        // -------------------------
        // 6. Calculate process-based completion (%)
        // -------------------------
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

        $overallPercentage = $totalMax
            ? round(($totalUserMark / $totalMax) * 100, 1)
            : 0;

        return [
            // ------------------------------------
            // STAT 1: Total Projects
            // ------------------------------------
            Stat::make('Total Projects', $totalProjects)
                ->description('All projects you created')
                ->descriptionIcon('heroicon-o-folder'),

            // ------------------------------------
            // STAT 2: Target (PLATINUM / GOLD / etc)
            // ------------------------------------
            Stat::make('Target', $latestTarget)
                ->description('Target of selected project')
                ->descriptionIcon('heroicon-o-flag')
                ->color(match($latestTarget) {
                    'PLATINUM' => 'success',
                    'GOLD'     => 'warning',
                    'SILVER'   => 'danger',
                    'CERTIFIED'=> 'primary',
                    default    => 'secondary',
                }),

            // ------------------------------------
            // STAT 3: Project Completion (%)
            // ------------------------------------
            Stat::make('Project Completion', $overallPercentage . '%')
                ->description('Progress based on process marks')
                ->descriptionIcon('heroicon-o-chart-pie')
                ->color(
                    $overallPercentage >= 80 ? 'success' :
                    ($overallPercentage >= 50 ? 'warning' : 'danger')
                ),

            // ------------------------------------
            // STAT 4: Status (from Process or Project)
            // ------------------------------------
            Stat::make('Status', $status)
                ->description('Final status of selected project')
                ->descriptionIcon('heroicon-o-check-circle')
                ->color(match($status) {
                    'PLATINUM'  => 'success',
                    'GOLD'      => 'warning',
                    'SILVER'    => 'primary',
                    'CERTIFIED' => 'success',
                    'FAIL'      => 'danger',
                    default     => 'secondary',
                }),
        ];
    }
}
