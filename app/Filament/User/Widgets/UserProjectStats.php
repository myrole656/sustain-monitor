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

    protected function getStats(): array
    {
       $userId = Auth::id();

        // Total projects
        $totalProjects = Project::where('user_id', $userId)->count();

        // Latest project
        $latestProject = Project::where('user_id', $userId)->latest()->first();

        // Target (string: PLATINUM, GOLD, SILVER)
        $latestTarget = $latestProject ? $latestProject->target : 'N/A';

        // Latest process for the user
        $latestProcess = Process::whereHas('project', fn($q) => $q->where('user_id', $userId))
            ->latest()
            ->first();

        // Latest status
        $status = $latestProcess ? $latestProcess->status : 'N/A';

        // Latest process for the user
        $process = Process::whereHas('project', function ($q) use ($userId) {
            $q->where('user_id', $userId);
        })->latest()->first();

      



        // Max scores per stage
        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 26,
            'Execution'  => 42,
            'Monitoring' => 12,
            'Closing'    => 14,
        ];

        // User marks (if process exists)
        $stepMarks = $process ? [
            'Initiation' => $process->initiation,
            'Planning'   => $process->planning,
            'Execution'  => $process->execution,
            'Monitoring' => $process->monitoring,
            'Closing'    => $process->closing,
        ] : array_fill_keys(array_keys($maxMarks), 0);

        // Overall completion %
        $totalMax = array_sum($maxMarks);
        $totalUserMark = array_sum($stepMarks);
        $overallPercentage = $totalMax ? round(($totalUserMark / $totalMax) * 100, 1) : 0;

        return [
            Stat::make('Total Projects', $totalProjects)
                ->description('All projects you created')
                ->descriptionIcon('heroicon-o-folder'),

            Stat::make('Target', $latestTarget)
                ->description('Target of latest project')
                ->descriptionIcon('heroicon-o-flag')
                ->color(match($latestTarget) {
                    'PLATINUM' => 'success',
                    'GOLD' => 'warning',
                    'SILVER' => 'danger',
                    default => 'primary',
                }),

            Stat::make('Project Completion', $overallPercentage . '%')
                ->description('Overall progress from process marks')
                ->descriptionIcon('heroicon-o-chart-pie')
                ->color(
                    $overallPercentage >= 80 ? 'success' :
                    ($overallPercentage >= 50 ? 'warning' : 'danger')
                ),

            Stat::make('Latest Status', $status)
                ->description('Status of the latest process')
                 ->descriptionIcon('heroicon-o-chart-pie')
                ->color(match($status) {
                    'PLATINUM', 'CERTIFIED' => 'success',
                    'GOLD' => 'warning',
                    'SILVER' => 'primary',
                    'FAIL' => 'danger',
                    default => 'secondary',
                }),


        ];
    }
}
