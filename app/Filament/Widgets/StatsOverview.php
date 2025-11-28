<?php

namespace App\Filament\Widgets;

use Filament\Widgets\StatsOverviewWidget;
use Filament\Widgets\StatsOverviewWidget\Stat;
use App\Models\Process;
use App\Models\User;

class StatsOverview extends StatsOverviewWidget
{
    protected ?string $heading = 'Project Status Overview';

    // Force this widget to be full width so it appears on top
    protected int | string | array $columnSpan = 'full';

    protected function getStats(): array
    {
        $platinumCount = Process::where('status', 'PLATINUM')->count();
        $failCount     = Process::where('status', 'FAIL')->count();

        $userCount = User::where('role', 'user')->count();


        return [
            Stat::make('PLATINUM Projects', $platinumCount)
                ->description('Projects achieving PLATINUM status')
                ->descriptionIcon('heroicon-o-star')
                ->color('success'),

            Stat::make('Failed Projects', $failCount)
                ->description('Projects that have failed')
                ->descriptionIcon('heroicon-o-x-circle')
                ->color('danger'),

            Stat::make('Total Users', $userCount)
                ->description('User')
                ->descriptionIcon('heroicon-o-user-group')
                ->color('primary'),
        ];
    }
}
