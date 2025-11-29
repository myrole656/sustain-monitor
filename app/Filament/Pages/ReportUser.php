<?php

namespace App\Filament\Pages;

use Filament\Pages\Page;
use App\Filament\Widgets\UserInfo;
use App\Filament\User\Widgets\ProjectDropDown;
use App\Filament\User\Widgets\UserProjectStats;
use App\Filament\User\Widgets\ResultChart;
use App\Filament\User\Widgets\TargetProject;
use App\Filament\User\Widgets\SDG;
use App\Filament\User\Widgets\OpenPdfWidget;
use UnitEnum; 
use BackedEnum;

class ReportUser extends Page
{
    protected string $view = 'filament.pages.report-user';
    protected static BackedEnum|string|null $navigationIcon = 'heroicon-o-users';

    public function getColumns(): array | int | string
    {
        return 12; // enable 12-column grid
    }

    protected function getHeaderWidgets(): array
    {
        return [
            ProjectDropDown::class,
            OpenPdfWidget::class,
            UserProjectStats::class,
            ResultChart::class,
            TargetProject::class,
            SDG::class,
        ];
    }

    // ✅ Correct method for header widget column spans
    public function getHeaderWidgetsColumnSpan(): array
    {
        return [
            ProjectDropDown::class => 6,
            OpenPdfWidget::class => 6,
        ];
    }
}
