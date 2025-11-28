<?php

namespace App\Filament\Widgets;

use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
use App\Models\Project;
use App\Models\Process;
use Illuminate\Support\Facades\Auth;

class UserInfo extends StatsOverviewWidget
{
    protected ?string $heading = 'Project Details';

    // Store the selected project ID
    public ?int $projectId = null;

    // Listen for the dropdown event
    protected $listeners = ['projectSelected' => 'updateProject'];

    // Update the selected project when dropdown changes
    public function updateProject($projectId)
    {
        $this->projectId = $projectId;
        $this->dispatch('$refresh');
    }

    protected function getStats(): array
    {
        $user = Auth::user();

        // If admin, allow viewing any project
        if ($user->role === 'admin') {
            $project = $this->projectId 
                ? Project::find($this->projectId)
                : Project::latest()->first();
        } else {
            // Normal users can only see their own projects
            $project = $this->projectId 
                ? Project::where('user_id', $user->id)->find($this->projectId)
                : Project::where('user_id', $user->id)->latest()->first();
        }

        $projectId = $project?->id ?? null;

        // Get the first process for the selected project
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

        // Display stats
        return [
            Stat::make('Project Name', $project?->project_name ?? 'N/A')
                ->description('Selected Project'),

            Stat::make('Target', $project?->target ?? 'N/A')
                ->description('Target Level'),

            Stat::make('Status', $process?->status ?? $project?->status ?? 'N/A')
                ->description('Current Status'),

            Stat::make('Completion', $overallPercentage . '%')
                ->description('Progress based on process marks'),
        ];
    }
}
