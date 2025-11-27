<?php

namespace App\Filament\Pages;

use Filament\Pages\Page;
use App\Models\Process;
use UnitEnum; 
use BackedEnum;


class ProjectStatus extends Page
{
    protected static ?string $title = 'Project Status Info';
    protected static ?string $slug = 'project-status';
protected static BackedEnum|string|null $navigationIcon = 'heroicon-o-clipboard-document';
    // This view will render the info list
    protected string $view = 'filament.pages.project-status';

    public function getStatusCounts(): array
    {
        return Process::select('status')
            ->selectRaw('COUNT(*) as total')
            ->groupBy('status')
            ->pluck('total', 'status')
            ->toArray();
    }
}
